    using System;
    using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
    
    public class Order
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime OrderDate { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime RequireDate { get; set; }
        public string Receiver { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }

        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }