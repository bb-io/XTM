using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Customers;
using Apps.XTM.Models.Response.Customers;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class CustomersActions
{
    #region Fields

    private static XTMClient _client;

    #endregion

    #region Constructors

    static CustomersActions()
    {
        _client = new();
    }

    #endregion

    #region Actions

    [Action("List customers", Description = "List all customers")]
    public async Task<ListCustomersResponse> ListCustomers(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders)
    {
        var endpoint = $"{ApiEndpoints.Customers}?activity=ALL";
        var response = await _client.ExecuteXtm<List<ManageCustomersResponse>>(endpoint,
            Method.Get,
            bodyObj: null,
            authenticationCredentialsProviders.ToArray());
        
        return new(response);
    }
    
    [Action("Create customer", Description = "Create new customer")]
    public Task<ManageCustomersResponse> CreateCustomer(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] CreateCustomerRequest input)
    {
        return _client.ExecuteXtm<ManageCustomersResponse>($"{ApiEndpoints.Customers}",
            Method.Post,
            bodyObj: input,
            authenticationCredentialsProviders.ToArray());
    }
    
    [Action("Get customer", Description = "Get specific customer by id")]
    public Task<CustomerResponse> GetCustomer(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] CustomerRequest customer)
    {
        return _client.ExecuteXtm<CustomerResponse>($"{ApiEndpoints.Customers}/{customer.CustomerId}",
            Method.Get,
            bodyObj: null,
            authenticationCredentialsProviders.ToArray());
    }    
    
    [Action("Update customer", Description = "Update specific customer")]
    public Task<ManageCustomersResponse> UpdateCustomer(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] CustomerRequest customer,
        [ActionParameter] UpdateCustomerRequest input)
    {
        return _client.ExecuteXtm<ManageCustomersResponse>($"{ApiEndpoints.Customers}/{customer.CustomerId}",
            Method.Put,
            bodyObj: input,
            authenticationCredentialsProviders.ToArray());
    }  
    
    [Action("Delete customer", Description = "Delete specific customer")]
    public Task DeleteCustomer(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] CustomerRequest customer)
    {
        return _client.ExecuteXtm($"{ApiEndpoints.Customers}/{customer.CustomerId}",
            Method.Delete,
            bodyObj: null,
            authenticationCredentialsProviders.ToArray());
    }

    #endregion
}