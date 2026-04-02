using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FinanzRechner.Domain.Enums
{
    public enum OperationType
    {
        [Display(Name = "Sägen")]
        Cutting = 1, // Распиловка

        [Display(Name = "Drehen")]
        Turning = 2, // Токарная обработка

        [Display(Name = "Fräsen")]
        Milling = 3, // Фрезерная обработка

        [Display(Name = "Bohren")]
        Drilling = 4, // Сверление

        [Display(Name = "Schweißen")]
        Welding = 5, // Сварка

        [Display(Name = "Waschen")]
        Washing = 6, // Мойка

        [Display(Name = "Kennzeichnen")]
        Marking = 7, // Маркировка

        [Display(Name = "Vormontage")]
        Subassembly = 8, // Подсборка

        [Display(Name = "Montage")]
        Assembly = 9, // Сборка

        [Display(Name = "Lackierung")]
        Painting = 10, // Покраска

        [Display(Name = "Prüfung")]
        Testing = 11, // Испытание

        [Display(Name = "Verpackung")]
        Packaging = 12, // Упаковка
    }
}