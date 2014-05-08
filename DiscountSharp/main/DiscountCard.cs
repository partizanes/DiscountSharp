using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscountSharp.main
{
    class DiscountCard
    {
        private int cardId { get; set; }
        private int cardSum {get; set;}
        private int cardStatus { get; set; }
        private int cardPercent { get; set; }
        private DateTime cardLastUpdate { get; set; }
    }
}
