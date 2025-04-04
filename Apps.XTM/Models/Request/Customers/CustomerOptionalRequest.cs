﻿using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Customers;

public class CustomerOptionalRequest
{
    [Display("Customer ID")]
    [DataSource(typeof(CustomerDataHandler))]
    public string? CustomerId { get; set; }
}