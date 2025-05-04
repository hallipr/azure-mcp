// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine.Parsing;
using AzureMcp.Arguments.Storage.Account;
using AzureMcp.Models.Argument;
using AzureMcp.Models.Command;
using AzureMcp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AzureMcp.Commands.Storage.Account;

public sealed class AccountListCommand(ILogger<AccountListCommand> logger) : SubscriptionCommand<AccountListArguments>()
{
    private readonly ILogger<AccountListCommand> _logger = logger;

    protected override string GetCommandName() => "list";

    protected override string GetCommandDescription() =>
        $"""
        List all Storage accounts in a subscription. This command retrieves all Storage accounts available
        in the specified {ArgumentDefinitions.Common.SubscriptionName}. Results include account names and are
        returned as a JSON array.
        """;

    [McpServerTool(Destructive = false, ReadOnly = true)]
    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var args = BindArguments(parseResult);

        try
        {
            if (!await ProcessArguments(context, args))
            {
                return context.Response;
            }

            var storageService = context.GetService<IStorageService>();
            var accounts = await storageService.GetStorageAccounts(
                args.Subscription!,
                args.Tenant,
                args.RetryPolicy);

            context.Response.Results = accounts?.Count > 0
                ? ResponseResult.Create(new Result(accounts), StorageJsonContext.Default.AccountListCommandResult)
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing storage accounts");
            HandleException(context.Response, ex);
        }

        return context.Response;
    }

    internal record Result(List<string> Accounts);
}
