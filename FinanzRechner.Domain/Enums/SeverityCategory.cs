using System;
using System.Collections.Generic;
using System.Text;

namespace FinanzRechner.Domain.Enums
{
    public enum SeverityCategory
    {
        // Легкие физические работы
        Ia = 1, // до 120 ккал/ч
        Ib = 2, // 121–150 ккал/ч

        // Физические работы средней тяжести
        IIa = 3, // 151–200 ккал/ч
        IIb = 4, // 201–250 ккал/ч

        // Тяжелые физические работы
        III = 5  // более 250 ккал/ч
    }
}
