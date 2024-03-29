﻿using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Projects;

public class CloneProjectRequest
{
    public string? Name { get; set; }
    
    [Display("Origin")] 
    [DataSource(typeof(ProjectDataHandler))]
    public string OriginId { get; set; }
}