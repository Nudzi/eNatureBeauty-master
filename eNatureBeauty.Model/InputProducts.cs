﻿using System;
using System.Collections.Generic;
using System.Text;

namespace eNatureBeauty.Model
{
    public class InputProducts
    {
        public int Id { get; set; }
        public int InputId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
