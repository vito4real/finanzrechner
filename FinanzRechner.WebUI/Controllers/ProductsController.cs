using ClosedXML.Excel;
using FinanzRechner.Domain.Entities;
using FinanzRechner.Domain.Enums;
using FinanzRechner.Infrastructure;
using FinanzRechner.WebUI.ViewModels.Products;
using FinanzRechner.WebUI.ViewModels.Products.Bom;
using FinanzRechner.WebUI.ViewModels.Products.Bop;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Reflection;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace FinanzRechner.WebUI.Controllers
{
    public class ProductsController : Controller
    {
        private readonly FinancialCalcDbContext _context;

        public ProductsController(FinancialCalcDbContext context)
        {
            _context = context;
        }

        private static List<SelectListItem> BuildOperationOptions()
        {
            return Enum.GetValues(typeof(OperationType))
                .Cast<OperationType>()
                .Select(op => new SelectListItem
                {
                    Value = ((int)op).ToString(),
                    Text = $"{(int)op}. {op}"
                })
                .ToList();
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .AsNoTracking()
                .Include(p=>p.ProductMaterials)
                .ThenInclude(pm=>pm.Material)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            // BOM-дерево (вниз по структуре)
            var bomTree = await BuildBomTree(product.Id, 1); // 1 — parentMultiplier for exploded BOM

            // BOP (техпроцесс)
            var bopLines = await _context.ProductBopLines
                .AsNoTracking()
                .Include(x => x.Workstation) // Подгружаем станок
                .Include(x => x.JobPosition) // Подгружаем должность
                .Where(x => x.ProductId == product.Id)
                .OrderBy(x => x.Sequence)
                .Select(x => new ProductBopRouteViewModel
                {
                    Sequence = x.Sequence,
                    Operation = x.Operation,
                    WorkstationName = x.Workstation.DisplayName,
                    WorkstationRate = x.Workstation.MachineHourlyCost,
                    JobTitle = x.JobPosition.Title,
                    JobRate = x.JobPosition.FinalHourlyRate,
                    Duration = x.Duration,
                    TotalCost = x.TotalOperationCost // Используем твой расчетный метод из сущности
                })
                .ToListAsync();

            // Where-used
            var whereUsedLines = await _context.ProductBomLines
                .Where(b => b.ChildProductId == product.Id)
                .Include(b => b.ParentProduct)
                .OrderBy(b => b.ParentProduct.Designation)
                .ToListAsync();

            var whereUsed = whereUsedLines
                .Select(b => new ProductWhereUsedViewModel
                {
                    ParentProductId = b.ParentProductId,
                    ParentDesignation = b.ParentProduct.Designation,
                    ParentName = b.ParentProduct.Name,
                    Quantity = b.Quantity   // сколько текущего входит в родителя
                })
                .ToList();

            var materials = product.ProductMaterials.Select(pm => new ProductMaterialDetailViewModel
            {
                Id= pm.Material.Id,
                MaterialName = pm.Material.Name,
                Quantity = pm.Quantity,
                UnitPrice = pm.Material.UnitPrice
            }).OrderBy(m => m.MaterialName).ToList();

            var vm = new ProductDetailsViewModel
            {
                Id = product.Id,
                Designation = product.Designation,
                Name = product.Name,
                BomTree = bomTree,
                BopLines = bopLines,
                WhereUsed = whereUsed,
                Materials = materials,
            };

            return View(vm);
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            await FillProductsSelectList();
            await FillOperationSelectList();
            await FillMaterialsSelectList();
            await FillBopSelectLists();
            
            var vm = new ProductEditViewModel
            {
                BomLines = new List<ProductBomLineViewModel>(),
                BopLines = new List<ProductBopLineViewModel>
                {
                    // стартовая строка, чтобы таблица BOP не была пустой (по желанию)
                    new ProductBopLineViewModel { Sequence = 1 }
                },
                MaterialLines=new List<ProductMaterialLineViewModel>(),
                OperationOptions = BuildOperationOptions()
            };

            return View(vm);
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductEditViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await FillProductsSelectList();
                await FillMaterialsSelectList();
                await FillOperationSelectList();
                await FillBopSelectLists();

                vm.OperationOptions = BuildOperationOptions();
                return View(vm);
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Designation = vm.Designation,
                Name = vm.Name
            };

            _context.Products.Add(product);

            // 1) сохраняем продукт, чтобы точно был Id (хотя Guid уже есть, но оставим как у тебя)
            await _context.SaveChangesAsync();

            // ===== 2. BOM =====

            var bomLines = (vm.BomLines ?? new List<ProductBomLineViewModel>())
                .Where(b => b.ChildProductId.HasValue && b.Quantity > 0)
                .Select(b => new ProductBomLine
                {
                    Id = Guid.NewGuid(),
                    ParentProductId = product.Id,
                    ChildProductId = b.ChildProductId!.Value,
                    Quantity = b.Quantity
                })
                .ToList();

            if (bomLines.Count > 0)
                _context.ProductBomLines.AddRange(bomLines);

            // словарь: ChildProductId -> BomLineId (оставил твою заготовку под BOA)
            var bomByChild = bomLines.ToDictionary(b => b.ChildProductId, b => b.Id);

            // ===== 3. BOP =====
            // Операции техпроцесса (Sequence + Operation + Workstation + Job + Duration)
            // фильтруем пустые строки и нормализуем Sequence

            var bopInput = (vm.BopLines ?? new List<ProductBopLineViewModel>())
                .Where(l => l.Sequence > 0) // минимальная проверка
                .ToList();

            // Если хочешь строго: убирать дубли sequence и пустые операции
            // (OperationType enum всегда имеет значение, но может быть 0 если выбрали "пусто")
            // Добавлена проверка на наличие выбранного станка и длительности > 0
            bopInput = bopInput
                .Where(l => (int)l.Operation > 0 && l.WorkstationId != Guid.Empty && l.Duration > 0)
                .ToList();

            // Нормализация Sequence (опционально): отсортировать и перенумеровать 1..N
            bopInput = bopInput.OrderBy(l => l.Sequence).ToList();
            for (int i = 0; i < bopInput.Count; i++)
                bopInput[i].Sequence = i + 1;

            var bopLines = bopInput
                .Select(l => new ProductBopLine
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Sequence = l.Sequence,
                    Operation = l.Operation,
                    // Новые поля для связи с ресурсами и временем
                    Duration = l.Duration,
                    WorkstationId = l.WorkstationId,
                    JobPositionId = l.JobPositionId
                })
                .ToList();

            if (bopLines.Count > 0)
                _context.ProductBopLines.AddRange(bopLines);

            // ===== 3. МАТЕРИАЛЫ =====
            var materialLines = (vm.MaterialLines ?? new List<ProductMaterialLineViewModel>())
                .Where(m => m.MaterialId != Guid.Empty && m.Quantity > 0)
                .Select(m => new ProductMaterial
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    MaterialId = m.MaterialId,
                    Quantity = m.Quantity
                }).ToList();

            if (materialLines.Any())
                _context.ProductMaterials.AddRange(materialLines);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        private async Task FillBopSelectLists()
        {
            ViewData["WorkstationsSelectList"] = new SelectList(
                await _context.Workstations.OrderBy(w => w.Code).ToListAsync(),
                "Id", "DisplayName");
            ViewData["JobPositionsSelectList"] = new SelectList(
                await _context.JobPositions.OrderBy(j => j.Title).ToListAsync(),
                "Id", "Title");
        }
        private async Task FillMaterialsSelectList()
        {
            var materials = await _context.Materials
                .OrderBy(m => m.Name)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.MaterialsSelectList = new SelectList(materials, "Id", "Name");
        }
        private async Task FillProductsSelectList(Guid? currentProductId = null)
        {
            var products = await _context.Products
                .OrderBy(p => p.Designation)
                .ToListAsync();

            if (currentProductId.HasValue)
            {
                products = products
                    .Where(p => p.Id != currentProductId.Value)
                    .ToList();
            }

            ViewBag.ProductsSelectList =
                new SelectList(products, "Id", "Designation");
        }

        private Task FillOperationSelectList()
        {
            var ops = Enum.GetValues(typeof(OperationType))
                .Cast<OperationType>()
                .Select(op => new SelectListItem
                {
                    Value = ((int)op).ToString(),
                    Text = $"{(int)op}. {op}"
                })
                .ToList();

            // ТЫ: сохраняешь в VM (рекомендую так), либо ViewBag как с ProductsSelectList.
            // Если у тебя Fill... заполняет ViewBag, скажи — переделаю.
            ViewData["OperationOptions"] = ops;

            return Task.CompletedTask;
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Designation,Name")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return RedirectToAction(nameof(Index));
            }

            // 1. Проверяем, используется ли продукт как дочерний в чужих BOM (твоя логика)
            var bomAsChild = await _context.ProductBomLines
                .AnyAsync(b => b.ChildProductId == id);

            if (bomAsChild)
            {
                TempData["ErrorMessage"] =
                    "Нельзя удалить изделие, так как оно входит в состав других изделий. " +
                    "Сначала удалите его из соответствующих спецификаций (BOM).";
                return RedirectToAction(nameof(Delete));
            }

            // 2. Проверяем, используется ли продукт в заказах (решение бага с PostgresException)
            // Замени 'OrderItems' на правильное имя твоей таблицы позиций заказов
            var usedInOrders = await _context.OrderProducts.AnyAsync(oi => oi.ProductId == id);

            if (usedInOrders)
            {
                TempData["ErrorMessage"] =
                    "Невозможно удалить изделие, так как оно фигурирует в существующих заказах. " +
                    "Вы можете только отредактировать его или удалить сначала связанные заказы.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            // 3. Если проверки пройдены, удаляем внутренние зависимости продукта
            // Удаляем его собственный со   став (BOM)
            var ownBomLines = _context.ProductBomLines.Where(l => l.ParentProductId == id);
            _context.ProductBomLines.RemoveRange(ownBomLines);

            // Удаляем его собственный техпроцесс (BOP)
            var ownBopLines = _context.ProductBopLines.Where(l => l.ProductId == id);
            _context.ProductBopLines.RemoveRange(ownBopLines);

            // 4. Удаляем сам продукт
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<ProductBomTreeViewModel>> BuildBomTree(
            Guid productId,
            int parentMultiplier = 1,
            HashSet<Guid>? path = null)
        {
            path ??= new HashSet<Guid>();
            if (!path.Add(productId)) return new List<ProductBomTreeViewModel>();

            var bomLines = await _context.ProductBomLines
                .AsNoTracking()
                .Where(b => b.ParentProductId == productId)
                .Include(b => b.ChildProduct)
                .OrderBy(b => b.ChildProduct.Designation)
                .ToListAsync();

            var result = new List<ProductBomTreeViewModel>();

            foreach (var line in bomLines)
            {
                var effectiveQty = line.Quantity * parentMultiplier;

                // 1. Считаем материалы (сначала тянем в список, потом Sum)
                var materialsList = await _context.ProductMaterials
                    .AsNoTracking()
                    .Include(pm => pm.Material)
                    .Where(pm => pm.ProductId == line.ChildProductId)
                    .ToListAsync();
                decimal childMaterialsSum = materialsList.Sum(pm => pm.Quantity * pm.Material.UnitPrice);

                // 2. Считаем работы (BOP) через List, чтобы избежать ошибки трансляции
                var bopList = await _context.ProductBopLines
                    .AsNoTracking()
                    .Include(b => b.JobPosition)
                    .Include(b => b.Workstation)
                    .Where(b => b.ProductId == line.ChildProductId)
                    .ToListAsync();
                decimal childBopSum = bopList.Sum(b => b.TotalOperationCost);

                // 3. Рекурсия для вложенных продуктов
                var internalChildren = await BuildBomTree(line.ChildProductId, 1, path);
                decimal childBomSum = internalChildren.Sum(c => c.TotalPrice);

                var node = new ProductBomTreeViewModel
                {
                    ProductId = line.ChildProductId,
                    Designation = line.ChildProduct.Designation,
                    Name = line.ChildProduct.Name,
                    Quantity = effectiveQty,
                    UnitPrice = childMaterialsSum + childBopSum + childBomSum,
                    Children = await BuildBomTree(line.ChildProductId, effectiveQty, path)
                };

                result.Add(node);
            }

            path.Remove(productId);
            return result;
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(Guid id)
        {

            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var bomTree = await BuildBomTree(product.Id);
            var bopLines = await _context.ProductBopLines
                .AsNoTracking()
                .Include(x => x.Workstation)
                .Include(x => x.JobPosition)
                .Where(x => x.ProductId == id)
                .OrderBy(x => x.Sequence)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Детализация изделия");

                // Общие настройки стиля для итогов
                var subTotalStyle = workbook.Style;
                subTotalStyle.Font.Bold = true;
                subTotalStyle.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");

                ws.Cell("A1").Value = $"ПОЛНЫЙ РАСЧЕТ ЗАТРАТ: {product.Name}";
                ws.Cell("A1").Style.Font.Bold = true;
                ws.Cell("A2").Value = $"Обозначение: {product.Designation}";

                int currentRow = 4;

                // --- 1. МАТЕРИАЛЫ ---
                ws.Cell(currentRow, 1).Value = "1. ПРЯМЫЕ МАТЕРИАЛЫ";
                ws.Range(currentRow, 1, currentRow, 4).Merge().Style.Font.Bold = true;
                currentRow++;

                ws.Cell(currentRow, 1).Value = "Наименование";
                ws.Cell(currentRow, 2).Value = "Кол-во";
                ws.Cell(currentRow, 3).Value = "Цена (BYN)";
                ws.Cell(currentRow, 4).Value = "Сумма (BYN)";
                ws.Range(currentRow, 1, currentRow, 4).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                currentRow++;

                foreach (var m in product.ProductMaterials)
                {
                    ws.Cell(currentRow, 1).Value = m.Material.Name;
                    ws.Cell(currentRow, 2).Value = m.Quantity;
                    ws.Cell(currentRow, 3).Value = m.Material.UnitPrice;
                    ws.Cell(currentRow, 4).FormulaA1 = $"B{currentRow}*C{currentRow}";
                    currentRow++;
                }

                // ИТОГО ПО МАТЕРИАЛАМ
                decimal matSum = product.ProductMaterials.Sum(m => m.Quantity * m.Material.UnitPrice);
                ws.Cell(currentRow, 3).Value = "Итого по материалам:";
                ws.Cell(currentRow, 4).Value = matSum;
                ws.Range(currentRow, 1, currentRow, 4).Style.Font.Bold = true;
                ws.Cell(currentRow, 4).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                currentRow += 2;

                // --- 2. ВХОДЯЩИЕ КОМПОНЕНТЫ (BOM) ---
                ws.Cell(currentRow, 1).Value = "2. ВХОДЯЩИЕ УЗЛЫ И ДЕТАЛИ (BOM)";
                ws.Range(currentRow, 1, currentRow, 4).Merge().Style.Font.Bold = true;
                currentRow++;

                ws.Cell(currentRow, 1).Value = "Обозначение / Наименование";
                ws.Cell(currentRow, 2).Value = "Кол-во";
                ws.Cell(currentRow, 3).Value = "Себест. ед.";
                ws.Cell(currentRow, 4).Value = "Итого (BYN)";
                currentRow++;

                foreach (var node in bomTree)
                {
                    ws.Cell(currentRow, 1).Value = $"{node.Designation} / {node.Name}";
                    ws.Cell(currentRow, 2).Value = node.Quantity;
                    ws.Cell(currentRow, 3).Value = node.UnitPrice;
                    ws.Cell(currentRow, 4).Value = node.TotalPrice;
                    currentRow++;
                }

                // ИТОГО ПО BOM
                decimal bomSum = bomTree.Sum(x => x.TotalPrice);
                ws.Cell(currentRow, 3).Value = "Итого по компонентам:";
                ws.Cell(currentRow, 4).Value = bomSum;
                ws.Range(currentRow, 1, currentRow, 4).Style.Font.Bold = true;
                ws.Cell(currentRow, 4).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                currentRow += 2;

                // --- 3. ТЕХПРОЦЕСС (BOP) ---
                ws.Cell(currentRow, 1).Value = "3. ТЕХНОЛОГИЧЕСКИЕ ОПЕРАЦИИ (BOP)";
                ws.Range(currentRow, 1, currentRow, 4).Merge().Style.Font.Bold = true;
                currentRow++;

                ws.Cell(currentRow, 1).Value = "Операция [Оборудование]";
                ws.Cell(currentRow, 2).Value = "Время (мин)";
                ws.Cell(currentRow, 3).Value = "Тариф ч. (маш+чел)";
                ws.Cell(currentRow, 4).Value = "Стоимость (BYN)";
                currentRow++;

                foreach (var bop in bopLines)
                {
                    string opName = GetOperationDisplayName(bop.Operation);
                    ws.Cell(currentRow, 1).Value = $"{opName} [{bop.Workstation.DisplayName}]";
                    ws.Cell(currentRow, 2).Value = bop.Duration;
                    ws.Cell(currentRow, 3).Value = bop.Workstation.MachineHourlyCost + bop.JobPosition.FinalHourlyRate;
                    ws.Cell(currentRow, 4).Value = bop.TotalOperationCost;
                    currentRow++;
                }

                // ИТОГО ПО BOP
                decimal bopSum = bopLines.Sum(l => l.TotalOperationCost);
                ws.Cell(currentRow, 3).Value = "Итого по работам:";
                ws.Cell(currentRow, 4).Value = bopSum;
                ws.Range(currentRow, 1, currentRow, 4).Style.Font.Bold = true;
                ws.Cell(currentRow, 4).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                currentRow += 2;

                // --- ФИНАЛЬНЫЙ РЕЗУЛЬТАТ ---
                ws.Cell(currentRow, 1).Value = "ПОЛНАЯ ПРОИЗВОДСТВЕННАЯ СЕБЕСТОИМОСТЬ:";
                ws.Range(currentRow, 1, currentRow, 3).Merge().Style.Font.Bold = true;

                ws.Cell(currentRow, 4).Value = matSum + bomSum + bopSum;
                ws.Cell(currentRow, 4).Style.Font.Bold = true;
                ws.Cell(currentRow, 4).Style.Fill.BackgroundColor = XLColor.Yellow;
                ws.Cell(currentRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

                // Косметика: формат чисел и ширина колонок
                ws.Column(4).Style.NumberFormat.Format = "#,##0.00 \"BYN\"";
                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Report_{product.Designation}.xlsx");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToPdf(Guid id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var culture = new CultureInfo("de-DE");

            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var bomTree = await BuildBomTree(product.Id);
            var bopLines = await _context.ProductBopLines
                .AsNoTracking()
                .Include(x => x.Workstation)
                .Include(x => x.JobPosition)
                .Where(x => x.ProductId == id)
                .OrderBy(x => x.Sequence)
                .ToListAsync();

            decimal matSum = product.ProductMaterials.Sum(m => m.Quantity * m.Material.UnitPrice);
            decimal bomSum = bomTree.Sum(x => x.TotalPrice);
            decimal bopSum = bopLines.Sum(l => l.TotalOperationCost);

            // --- IMAGE: привязка к product.Designation ---
            byte[]? productImageBytes = null;
            var imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");

            if (Directory.Exists(imageDirectory))
            {
                var matchingImage = Directory.EnumerateFiles(imageDirectory, $"{product.Designation}*.*", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(f =>
                    {
                        var ext = Path.GetExtension(f).ToLowerInvariant();
                        return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp";
                    });

                if (!string.IsNullOrWhiteSpace(matchingImage))
                    productImageBytes = System.IO.File.ReadAllBytes(matchingImage);
            }

            // --- QR: ссылка на страницу продукта ---
            var productUrl = Url.Action(nameof(Details), "Products", new { id = product.Id }, Request.Scheme);

            byte[]? qrCodeBytes = null;
            if (!string.IsNullOrWhiteSpace(productUrl))
            {
                using var qrGenerator = new QRCoder.QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(productUrl, QRCoder.QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCoder.PngByteQRCode(qrData);
                qrCodeBytes = qrCode.GetGraphic(20);
            }

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("KOSTENKALKULATION")
                                .FontSize(18)
                                .SemiBold()
                                .FontColor(Colors.Blue.Medium);

                            col.Item().Text($"{product.Name} ({product.Designation})")
                                .FontSize(12)
                                .Italic();

                            col.Item().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm", culture))
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        });

                        if (productImageBytes is not null)
                        {
                            row.ConstantItem(150)
                                .Height(85)
                                .PaddingLeft(10)
                                .Image(productImageBytes, ImageScaling.FitArea);
                        }
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(15);

                        // --- 1. TABELLE MATERIALIEN ---
                        col.Item().Text("1. Direkte Materialien").FontSize(12).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn((float)1.5);
                                columns.RelativeColumn((float)1.5);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Bezeichnung");
                                header.Cell().Element(CellStyle).AlignRight().Text("Menge");
                                header.Cell().Element(CellStyle).AlignRight().Text("Einzelpreis");
                                header.Cell().Element(CellStyle).AlignRight().Text("Summe");
                            });

                            foreach (var m in product.ProductMaterials)
                            {
                                table.Cell().Element(CellStyle).Text(m.Material.Name);
                                table.Cell().Element(CellStyle).AlignRight().Text($"{m.Quantity.ToString("N2", culture)}");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{m.Material.UnitPrice.ToString("N2", culture)}");
                                table.Cell().Element(CellStyle).AlignRight().Text((m.Quantity * m.Material.UnitPrice).ToString("N2", culture));
                            }

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(3).Element(FooterStyle).AlignRight().Text("Gesamtkosten Materialien:");
                                footer.Cell().Element(FooterStyle).AlignRight().Text(matSum.ToString("N2", culture));
                            });
                        });

                        // --- 2. TABELLE BOM ---
                        col.Item().Text("2. Stückliste (BOM)").FontSize(12).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn((float)1.5);
                                columns.RelativeColumn((float)1.5);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Artikel / Bezeichnung");
                                header.Cell().Element(CellStyle).AlignRight().Text("Menge");
                                header.Cell().Element(CellStyle).AlignRight().Text("Stückkosten");
                                header.Cell().Element(CellStyle).AlignRight().Text("Summe");
                            });

                            foreach (var node in bomTree)
                            {
                                table.Cell().Element(CellStyle).Text($"{node.Designation} / {node.Name}");
                                table.Cell().Element(CellStyle).AlignRight().Text(node.Quantity.ToString("N2", culture));
                                table.Cell().Element(CellStyle).AlignRight().Text(node.UnitPrice.ToString("N2", culture));
                                table.Cell().Element(CellStyle).AlignRight().Text(node.TotalPrice.ToString("N2", culture));
                            }

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(3).Element(FooterStyle).AlignRight().Text("Gesamtkosten Komponenten:");
                                footer.Cell().Element(FooterStyle).AlignRight().Text(bomSum.ToString("N2", culture));
                            });
                        });

                        // --- 3. TABELLE BOP ---
                        col.Item().Text("3. Arbeitsgänge (BOP)").FontSize(12).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn((float)1.5);
                                columns.RelativeColumn((float)1.5);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Arbeitsgang [Arbeitsplatz]");
                                header.Cell().Element(CellStyle).AlignRight().Text("Arbeitszeit");
                                header.Cell().Element(CellStyle).AlignRight().Text("Stundensatz");
                                header.Cell().Element(CellStyle).AlignRight().Text("Kosten");
                            });

                            foreach (var bop in bopLines)
                            {
                                string opName = GetOperationDisplayName(bop.Operation);

                                table.Cell().Element(CellStyle).Text($"{opName} [{bop.Workstation.DisplayName}]");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{bop.Duration.ToString("N2", culture)} Min.");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{(bop.Workstation.MachineHourlyCost + bop.JobPosition.FinalHourlyRate).ToString("N2", culture)} BYN/h");
                                table.Cell().Element(CellStyle).AlignRight().Text(bop.TotalOperationCost.ToString("N2", culture));
                            }

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(3).Element(FooterStyle).AlignRight().Text("Gesamtkosten Arbeit:");
                                footer.Cell().Element(FooterStyle).AlignRight().Text(bopSum.ToString("N2", culture));
                            });
                        });

                        // --- ИТОГ + QR справа, как на желаемом макете ---
                        col.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem();

                            row.ConstantItem(290).Column(right =>
                            {
                                right.Spacing(8);

                                right.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn();
                                        c.RelativeColumn();
                                    });

                                    table.Cell().Background(Colors.Blue.Lighten5).Padding(5).Text("Gesamtkosten:").SemiBold();
                                    table.Cell()
                                        .Background(Colors.Blue.Lighten4)
                                        .Padding(5)
                                        .AlignRight()
                                        .Text($"{(matSum + bomSum + bopSum).ToString("N2", culture)} BYN")
                                        .SemiBold();
                                });

                                if (qrCodeBytes is not null)
                                {
                                    right.Item()
                                        .AlignRight()
                                        .Width(80)
                                        .Height(80)
                                        .Image(qrCodeBytes, ImageScaling.FitArea);
                                }
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Seite ");
                        x.CurrentPageNumber();
                    });
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Kostenkalkulation_{product.Designation}.pdf");

            static IContainer CellStyle(IContainer container) =>
                container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(2);

            static IContainer FooterStyle(IContainer container) =>
                container.PaddingVertical(5).PaddingHorizontal(2);
        }

        [HttpGet]
    public async Task<IActionResult> ExportToWord(Guid id)
    {
        // ВНИМАНИЕ: Если используете версию 2.0+, может потребоваться лицензия. 
        // Рекомендуется версия 1.8.0 или удаление строки с лицензией, если пакет бесплатный.

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        var bomTree = await BuildBomTree(product.Id);
        var bopLines = await _context.ProductBopLines
            .AsNoTracking()
            .Include(x => x.Workstation).Include(x => x.JobPosition)
            .Where(x => x.ProductId == id).OrderBy(x => x.Sequence).ToListAsync();

        using (var stream = new MemoryStream())
        {
            using (var doc = DocX.Create(stream))
            {
                // --- ЗАГОЛОВОК ---
                var title = doc.InsertParagraph("ОТЧЕТ ПО СЕБЕСТОИМОСТИ ИЗДЕЛИЯ").FontSize(18).Bold().Alignment = Alignment.center;
                doc.InsertParagraph($"Изделие: {product.Name}").FontSize(12).SpacingBefore(10);
                doc.InsertParagraph($"Обозначение: {product.Designation}").FontSize(12);
                doc.InsertParagraph($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10).Italic().SpacingAfter(20);

                // --- 1. СЕКЦИЯ: МАТЕРИАЛЫ ---
                doc.InsertParagraph("1. Прямые материалы").Bold().FontSize(14).SpacingAfter(10);
                var matTable = doc.AddTable(product.ProductMaterials.Count + 2, 4);
                matTable.Design = TableDesign.TableGrid;
                matTable.Alignment = Alignment.center;

                // Шапка материалов
                matTable.Rows[0].Cells[0].Paragraphs[0].Append("Наименование").Bold();
                matTable.Rows[0].Cells[1].Paragraphs[0].Append("Кол-во").Bold();
                matTable.Rows[0].Cells[2].Paragraphs[0].Append("Цена (BYN)").Bold();
                matTable.Rows[0].Cells[3].Paragraphs[0].Append("Сумма (BYN)").Bold();

                decimal matTotal = 0;
                for (int i = 0; i < product.ProductMaterials.Count; i++)
                {
                    var m = product.ProductMaterials.ElementAt(i);
                    var rowSum = m.Quantity * m.Material.UnitPrice;
                    matTotal += rowSum;

                    matTable.Rows[i + 1].Cells[0].Paragraphs[0].Append(m.Material.Name);
                    matTable.Rows[i + 1].Cells[1].Paragraphs[0].Append(m.Quantity.ToString("G29"));
                    matTable.Rows[i + 1].Cells[2].Paragraphs[0].Append(m.Material.UnitPrice.ToString("N2"));
                    matTable.Rows[i + 1].Cells[3].Paragraphs[0].Append(rowSum.ToString("N2"));
                }

                // Итог по материалам
                var matFooter = matTable.Rows[product.ProductMaterials.Count + 1];
                matFooter.Cells[2].Paragraphs[0].Append("Итого:").Bold();
                matFooter.Cells[3].Paragraphs[0].Append(matTotal.ToString("N2")).Bold();
                doc.InsertTable(matTable);

                // --- 2. СЕКЦИЯ: BOM (Входящие компоненты) ---
                doc.InsertParagraph("\n2. Состав изделия (BOM)").Bold().FontSize(14).SpacingAfter(10);
                var bomTable = doc.AddTable(bomTree.Count + 2, 4);
                bomTable.Design = TableDesign.TableGrid;

                bomTable.Rows[0].Cells[0].Paragraphs[0].Append("Обозначение / Название").Bold();
                bomTable.Rows[0].Cells[1].Paragraphs[0].Append("Кол-во").Bold();
                bomTable.Rows[0].Cells[2].Paragraphs[0].Append("Себест. ед.").Bold();
                bomTable.Rows[0].Cells[3].Paragraphs[0].Append("Итого (BYN)").Bold();

                decimal bomTotal = 0;
                for (int i = 0; i < bomTree.Count; i++)
                {
                    var node = bomTree[i];
                    bomTotal += node.TotalPrice;

                    bomTable.Rows[i + 1].Cells[0].Paragraphs[0].Append($"{node.Designation} / {node.Name}");
                    bomTable.Rows[i + 1].Cells[1].Paragraphs[0].Append(node.Quantity.ToString());
                    bomTable.Rows[i + 1].Cells[2].Paragraphs[0].Append(node.UnitPrice.ToString("N2"));
                    bomTable.Rows[i + 1].Cells[3].Paragraphs[0].Append(node.TotalPrice.ToString("N2"));
                }

                var bomFooter = bomTable.Rows[bomTree.Count + 1];
                bomFooter.Cells[2].Paragraphs[0].Append("Итого:").Bold();
                bomFooter.Cells[3].Paragraphs[0].Append(bomTotal.ToString("N2")).Bold();
                doc.InsertTable(bomTable);

                // --- 3. СЕКЦИЯ: BOP (Технологические операции) ---
                doc.InsertParagraph("\n3. Технологические операции (BOP)").Bold().FontSize(14).SpacingAfter(10);
                var bopTable = doc.AddTable(bopLines.Count + 2, 4);
                bopTable.Design = TableDesign.TableGrid;

                bopTable.Rows[0].Cells[0].Paragraphs[0].Append("Операция [Оборудование]").Bold();
                bopTable.Rows[0].Cells[1].Paragraphs[0].Append("Время (мин)").Bold();
                bopTable.Rows[0].Cells[2].Paragraphs[0].Append("Тариф ч.").Bold();
                bopTable.Rows[0].Cells[3].Paragraphs[0].Append("Стоимость (BYN)").Bold();

                decimal bopTotal = 0;
                for (int i = 0; i < bopLines.Count; i++)
                {
                    var line = bopLines[i];
                    bopTotal += line.TotalOperationCost;
                    string opName = GetOperationDisplayName(line.Operation); // Используем наш метод перевода

                    bopTable.Rows[i + 1].Cells[0].Paragraphs[0].Append($"{opName} [{line.Workstation.DisplayName}]");
                    bopTable.Rows[i + 1].Cells[1].Paragraphs[0].Append(line.Duration.ToString("N2"));
                    bopTable.Rows[i + 1].Cells[2].Paragraphs[0].Append((line.Workstation.MachineHourlyCost + line.JobPosition.FinalHourlyRate).ToString("N2"));
                    bopTable.Rows[i + 1].Cells[3].Paragraphs[0].Append(line.TotalOperationCost.ToString("N2"));
                }

                var bopFooter = bopTable.Rows[bopLines.Count + 1];
                bopFooter.Cells[2].Paragraphs[0].Append("Итого:").Bold();
                bopFooter.Cells[3].Paragraphs[0].Append(bopTotal.ToString("N2")).Bold();
                doc.InsertTable(bopTable);

                // --- ИТОГОВЫЙ РАСЧЕТ ---
                var totalParagraph = doc.InsertParagraph($"\nПОЛНАЯ СЕБЕСТОИМОСТЬ: {(matTotal + bomTotal + bopTotal):N2} BYN");
                totalParagraph.Bold().FontSize(16).Alignment = Alignment.right;
                totalParagraph.SpacingBefore(30);

                doc.Save();
            }
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"FullReport_{product.Designation}.docx");
        }
    }

    private string GetOperationDisplayName(OperationType op)
        {
            return op.GetType()
                .GetMember(op.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>()?.Name ?? op.ToString();

        }
        private bool ProductExists(Guid id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
