using System;
using System.Collections.Generic;

namespace Northwind.Mvc.Models;

public partial class ShipperContactInfo
{
    public int ContactInfoId { get; set; }

    public int ShipperId { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public string? PostalCode { get; set; }

    public bool IsActive { get; set; }

    public virtual Shipper Shipper { get; set; } = null!;
}
