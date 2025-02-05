﻿using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Customers;

public class CustomerRequest
{
    [Display("Customer ID")]
    [DataSource(typeof(CustomerDataHandler))]
    public string CustomerId { get; set; } = string.Empty;
}