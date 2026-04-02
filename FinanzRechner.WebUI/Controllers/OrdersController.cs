using FinanzRechner.Domain.Entities;
using FinanzRechner.Infrastructure;
using FinanzRechner.WebUI.ViewModels;
using FinanzRechner.WebUI.ViewModels.Products.Bom;
using FinanzRechner.WebUI.ViewModels.Products.Order;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xceed.Document.NET;

namespace FinanzRechner.WebUI.Controllers
{
    public class OrdersController : Controller
    {
        private readonly FinancialCalcDbContext _context;

        public OrdersController(FinancialCalcDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var financialCalcDbContext = _context.Orders.Include(o => o.Client);
            return View(await financialCalcDbContext.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Client)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            var productItems = new List<OrderProductItemViewModel>();

            foreach (var op in order.OrderProducts)
            {
                // 1. Считаем материалы самого изделия
                var materials = await _context.ProductMaterials
                    .AsNoTracking()
                    .Include(pm => pm.Material)
                    .Where(pm => pm.ProductId == op.ProductId)
                    .ToListAsync();
                decimal materialsCost = materials.Sum(pm => pm.Quantity * pm.Material.UnitPrice);

                // 2. Считаем техпроцесс (BOP) изделия
                var bopLines = await _context.ProductBopLines
                    .AsNoTracking()
                    .Include(b => b.JobPosition)
                    .Include(b => b.Workstation)
                    .Where(b => b.ProductId == op.ProductId)
                    .ToListAsync();
                decimal bopCost = bopLines.Sum(b => b.TotalOperationCost);

                // 3. Считаем вложенные изделия (BOM) через рекурсию
                var bomTree = await BuildBomTree(op.ProductId, 1);
                decimal bomCost = bomTree.Sum(b => b.TotalPrice);

                decimal unitCost = materialsCost + bopCost + bomCost;

                productItems.Add(new OrderProductItemViewModel
                {
                    ProductId = op.ProductId,
                    ProductDesignation = op.Product.Designation,
                    ProductName = op.Product.Name,
                    Quantity = op.Quantity,
                    UnitPrice = unitCost,
                    TotalPrice = unitCost * op.Quantity
                });
            }

            var vm = new OrderDetailsViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                ClientName = order.Client.Name,
                Products = productItems,
                TotalOrderCost = productItems.Sum(p => p.TotalPrice)
            };

            return View(vm);
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

                // Расчет "на лету" для каждого вложенного узла
                var childMaterials = await _context.ProductMaterials
                    .AsNoTracking()
                    .Include(pm => pm.Material)
                    .Where(pm => pm.ProductId == line.ChildProductId)
                    .ToListAsync();

                var childBop = await _context.ProductBopLines
                    .AsNoTracking()
                    .Include(b => b.JobPosition)
                    .Include(b => b.Workstation)
                    .Where(b => b.ProductId == line.ChildProductId)
                    .ToListAsync();

                var subChildren = await BuildBomTree(line.ChildProductId, 1, path);

                var node = new ProductBomTreeViewModel
                {
                    ProductId = line.ChildProductId,
                    Designation = line.ChildProduct.Designation,
                    Name = line.ChildProduct.Name,
                    Quantity = effectiveQty,
                    UnitPrice = childMaterials.Sum(m => m.Quantity * m.Material.UnitPrice) +
                                childBop.Sum(b => b.TotalOperationCost) +
                                subChildren.Sum(s => s.TotalPrice)
                };

                result.Add(node);
            }

            path.Remove(productId);
            return result;
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            await FillOrderSelectLists();

            var vm = new OrderEditViewModel
            {
                OrderDate = DateTime.Now,
                Items = new List<OrderProductLineViewModel>
                {
                    new OrderProductLineViewModel {Quantity = 1},
                }
            }; 

            return View(vm);
        }
        private async Task FillOrderSelectLists()
        {
            var clients = await _context.Clients
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            var products = await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Designation)
                .ToListAsync();

            ViewData["ClientId"]=new SelectList(clients, "Id", "Name");
            ViewData["ProductId"] = new SelectList(products, "Id", "Designation");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderEditViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ClientId"] = new SelectList(_context.Clients.AsNoTracking(), "Id", "Name", vm.ClientId);
                ViewData["ProductId"] = new SelectList(_context.Clients.AsNoTracking(), "Id", "Designation");
                return View(vm);
            }
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = vm.OrderNumber,
                ClientId = vm.ClientId,
            };

            _context.Orders.Add(order);

            if(vm.Items !=null && vm.Items.Any())
            {
                foreach (var item in vm.Items)
                {
                    if (item.ProductId == null || item.ProductId == Guid.Empty)
                        continue;

                    var orderProduct = new OrderProduct
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = item.ProductId.Value,
                        Quantity = item.Quantity,

                    }; 

                    _context.OrderProducts.Add(orderProduct);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name", order.ClientId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,OrderNumber,ClientId")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
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
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name", order.ClientId);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(Guid id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }

        private async Task<OrderDetailsViewModel> GetOrderDetailsData(Guid id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Client)
                .Include(o => o.OrderProducts).ThenInclude(op => op.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return null;

            var productItems = new List<OrderProductItemViewModel>();
            foreach (var op in order.OrderProducts)
            {
                var materials = await _context.ProductMaterials.AsNoTracking()
                    .Include(pm => pm.Material).Where(pm => pm.ProductId == op.ProductId).ToListAsync();
                var bopLines = await _context.ProductBopLines.AsNoTracking()
                    .Include(b => b.JobPosition).Include(b => b.Workstation).Where(b => b.ProductId == op.ProductId).ToListAsync();
                var bomTree = await BuildBomTree(op.ProductId, 1);

                decimal unitCost = materials.Sum(m => m.Quantity * m.Material.UnitPrice) +
                                   bopLines.Sum(b => b.TotalOperationCost) +
                                   bomTree.Sum(b => b.TotalPrice);

                productItems.Add(new OrderProductItemViewModel
                {
                    ProductDesignation = op.Product.Designation,
                    ProductName = op.Product.Name,
                    Quantity = op.Quantity,
                    UnitPrice = unitCost,
                    TotalPrice = unitCost * op.Quantity
                });
            }

            return new OrderDetailsViewModel
            {
                OrderNumber = order.OrderNumber,
                ClientName = order.Client.Name,
                Products = productItems,
                TotalOrderCost = productItems.Sum(p => p.TotalPrice)
            };
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(Guid id)
        {
            var data = await GetOrderDetailsData(id);
            if (data == null) return NotFound();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Заказ " + data.OrderNumber);
                ws.Cell("A1").Value = $"ЗАКАЗ № {data.OrderNumber}";
                ws.Cell("A2").Value = $"Клиент: {data.ClientName}";

                var headerRow = 4;
                ws.Cell(headerRow, 1).Value = "Обозначение";
                ws.Cell(headerRow, 2).Value = "Наименование";
                ws.Cell(headerRow, 3).Value = "Кол-во";
                ws.Cell(headerRow, 4).Value = "Цена ед.";
                ws.Cell(headerRow, 5).Value = "Итого";
                ws.Range(headerRow, 1, headerRow, 5).Style.Font.Bold = true;

                int currentRow = 5;
                foreach (var p in data.Products)
                {
                    ws.Cell(currentRow, 1).Value = p.ProductDesignation;
                    ws.Cell(currentRow, 2).Value = p.ProductName;
                    ws.Cell(currentRow, 3).Value = p.Quantity;
                    ws.Cell(currentRow, 4).Value = p.UnitPrice;
                    ws.Cell(currentRow, 5).Value = p.TotalPrice;
                    currentRow++;
                }

                ws.Cell(currentRow, 4).Value = "ИТОГО ПО ЗАКАЗУ:";
                ws.Cell(currentRow, 5).Value = data.TotalOrderCost;
                ws.Range(currentRow, 4, currentRow, 5).Style.Font.Bold = true;

                ws.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Order_{data.OrderNumber}.xlsx");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToPdf(Guid id)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var data = await GetOrderDetailsData(id);
            if (data == null) return NotFound();

            var document = QuestPDF.Fluent.Document.Create(container => {
                container.Page(page => {
                    page.Margin(50);
                    page.Header().Text($"ЗАКАЗ № {data.OrderNumber}").FontSize(20).SemiBold();
                    page.Content().PaddingVertical(10).Column(col => {
                        col.Item().Text($"Клиент: {data.ClientName}").FontSize(12);
                        col.Item().Table(table => {
                            table.ColumnsDefinition(c => {
                                c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(1); c.RelativeColumn((float)1.5); c.RelativeColumn((float)1.5);
                            });
                            table.Header(h => {
                                h.Cell().Text("Обозначение"); h.Cell().Text("Имя"); h.Cell().Text("Кол-во"); h.Cell().Text("Цена"); h.Cell().Text("Итого");
                            });
                            foreach (var p in data.Products)
                            {
                                table.Cell().Text(p.ProductDesignation);
                                table.Cell().Text(p.ProductName);
                                table.Cell().Text(p.Quantity.ToString());
                                table.Cell().Text(p.UnitPrice.ToString("N2"));
                                table.Cell().Text(p.TotalPrice.ToString("N2"));
                            }
                        });
                        col.Item().AlignRight().Text($"ИТОГО: {data.TotalOrderCost:N2} BYN").FontSize(14).Bold();
                    });
                });
            });

            return File(document.GeneratePdf(), "application/pdf", $"Order_{data.OrderNumber}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ExportToWord(Guid id)
        {
            var data = await GetOrderDetailsData(id);
            if (data == null) return NotFound();

            using (var stream = new MemoryStream())
            {
                using (var doc = Xceed.Words.NET.DocX.Create(stream))
                {
                    doc.InsertParagraph($"СПЕЦИФИКАЦИЯ К ЗАКАЗУ № {data.OrderNumber}").FontSize(16).Bold().Alignment = Alignment.center;
                    doc.InsertParagraph($"Клиент: {data.ClientName}").SpacingAfter(20);

                    var table = doc.AddTable(data.Products.Count + 1, 5);
                    table.Design = TableDesign.TableGrid;
                    table.Rows[0].Cells[0].Paragraphs[0].Append("Обозначение").Bold();
                    table.Rows[0].Cells[1].Paragraphs[0].Append("Наименование").Bold();
                    table.Rows[0].Cells[2].Paragraphs[0].Append("Кол-во").Bold();
                    table.Rows[0].Cells[3].Paragraphs[0].Append("Цена ед.").Bold();
                    table.Rows[0].Cells[4].Paragraphs[0].Append("Сумма").Bold();

                    for (int i = 0; i < data.Products.Count; i++)
                    {
                        var p = data.Products[i];
                        table.Rows[i + 1].Cells[0].Paragraphs[0].Append(p.ProductDesignation);
                        table.Rows[i + 1].Cells[1].Paragraphs[0].Append(p.ProductName);
                        table.Rows[i + 1].Cells[2].Paragraphs[0].Append(p.Quantity.ToString());
                        table.Rows[i + 1].Cells[3].Paragraphs[0].Append(p.UnitPrice.ToString("N2"));
                        table.Rows[i + 1].Cells[4].Paragraphs[0].Append(p.TotalPrice.ToString("N2"));
                    }
                    doc.InsertTable(table);
                    doc.InsertParagraph($"\nОБЩАЯ СТОИМОСТЬ ЗАКАЗА: {data.TotalOrderCost:N2} BYN").Bold().Alignment = Alignment.right;

                    doc.Save();
                }
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Order_{data.OrderNumber}.docx");
            }
        }
    }
    
}
