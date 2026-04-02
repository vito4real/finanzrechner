using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FinanzRechner.Domain.Enums
{
    public enum OperationType
    {
        [Display(Name ="Распиловка")]
        Cutting = 1, // Распиловка

        [Display(Name = "Токарная обработка")]
        Turning = 2, // Токарная обработка

        [Display(Name = "Фрезерная обработка")]
        Milling = 3, // Фрезерная обработка

        [Display(Name = "Сверление")]
        Drilling = 4, // Сверление

        [Display(Name = "Сварка")]
        Welding = 5, // Сварка

        [Display(Name = "Мойка")]
        Washing = 6, // Мойка

        [Display(Name = "Маркировка")]
        Marking = 7, // Маркировка

        [Display(Name = "Подсборка")]
        Subassembly = 8, // Подсборка

        [Display(Name = "Сборка")]
        Assembly = 9, // Сборка

        [Display(Name = "Покраска")]
        Painting = 10, // Покраска

        [Display(Name = "Испытание")]
        Testing = 11, // Испытание

        [Display(Name = "Упаковка")]
        Packaging = 12, // Упаковка
    }
}