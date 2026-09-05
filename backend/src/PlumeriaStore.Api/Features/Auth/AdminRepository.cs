using Amazon.DynamoDBv2.Model;
using PlumeriaStore.Api.Common.Data;

namespace PlumeriaStore.Api.Features.Auth;

public sealed class AdminRepository
{
    private readonly DynamoTable _table;

    public AdminRepository(DynamoTable table)
    {
        _table = table;
    }

    public async Task<AdminUser?> FindAsync(CancellationToken cancellationToken = default)
    {
        var item = await _table.GetAsync(DynamoKeys.Admin, DynamoKeys.Admin, cancellationToken);
        return item is null ? null : FromItem(item);
    }

    public Task SaveAsync(AdminUser admin, CancellationToken cancellationToken = default) =>
        _table.PutAsync(ToItem(admin), cancellationToken: cancellationToken);

    /// <summary>
    /// Writes the account only if there isn't one, so a cold start that races another can't
    /// overwrite credentials the admin has since changed.
    /// </summary>
    public async Task<bool> TryCreateAsync(AdminUser admin, CancellationToken cancellationToken = default)
    {
        try
        {
            await _table.PutAsync(ToItem(admin), "attribute_not_exists(PK)", cancellationToken);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    private static Dictionary<string, AttributeValue> ToItem(AdminUser admin) => new()
    {
        [DynamoKeys.PartitionKey] = Attr.S(DynamoKeys.Admin),
        [DynamoKeys.SortKey] = Attr.S(DynamoKeys.Admin),
        ["Username"] = Attr.S(admin.Username),
        ["PasswordHash"] = Attr.S(admin.PasswordHash),
    };

    private static AdminUser FromItem(Dictionary<string, AttributeValue> item) => new()
    {
        Username = Attr.GetString(item, "Username"),
        PasswordHash = Attr.GetString(item, "PasswordHash"),
    };
}
