using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Customers;
using Apps.XTM.Models.Response.Customers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class CustomersActions : XtmInvocable
{
    public CustomersActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    #region Actions

    [Action("List customers", Description = "List all customers")]
    public async Task<ListCustomersResponse> ListCustomers()
    {
        var endpoint = $"{ApiEndpoints.Customers}?activity=ALL";
        var response = await Client.ExecuteXtmWithJson<List<ManageCustomersResponse>>(endpoint,
            Method.Get,
            null,
            Creds);

        return new(response);
    }

    [Action("Create customer", Description = "Create new customer")]
    public Task<ManageCustomersResponse> CreateCustomer([ActionParameter] CreateCustomerRequest input)
    {
        return Client.ExecuteXtmWithJson<ManageCustomersResponse>($"{ApiEndpoints.Customers}",
            Method.Post,
            input,
            Creds);
    }

    [Action("Get customer", Description = "Get specific customer by ID")]
    public Task<CustomerResponse> GetCustomer([ActionParameter] CustomerRequest customer)
    {
        return Client.ExecuteXtmWithJson<CustomerResponse>($"{ApiEndpoints.Customers}/{customer.CustomerId}",
            Method.Get,
            null,
            Creds);
    }

    [Action("Update customer", Description = "Update specific customer")]
    public Task<ManageCustomersResponse> UpdateCustomer(
        [ActionParameter] CustomerRequest customer,
        [ActionParameter] UpdateCustomerRequest input)
    {
        return Client.ExecuteXtmWithJson<ManageCustomersResponse>($"{ApiEndpoints.Customers}/{customer.CustomerId}",
            Method.Put,
            input,
            Creds);
    }

    [Action("Delete customer", Description = "Delete specific customer")]
    public Task DeleteCustomer([ActionParameter] CustomerRequest customer)
    {
        return Client.ExecuteXtmWithJson($"{ApiEndpoints.Customers}/{customer.CustomerId}",
            Method.Delete,
            null,
            Creds);
    }

    #endregion
}