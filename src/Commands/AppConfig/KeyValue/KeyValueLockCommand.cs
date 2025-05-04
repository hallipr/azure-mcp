// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine.Parsing;
using AzureMcp.Arguments.AppConfig.KeyValue;
using AzureMcp.Models.Command;
using AzureMcp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AzureMcp.Commands.AppConfig.KeyValue;

public sealed class KeyValueLockCommand(ILogger<KeyValueLockCommand> logger) : BaseKeyValueCommand<KeyValueLockArguments>()
{
    private readonly ILogger<KeyValueLockCommand> _logger = logger;

    protected override string GetCommandName() => "lock";

    protected override string GetCommandDescription() =>
        """
        Lock a key-value in an App Configuration store. This command sets a key-value to read-only mode,
        preventing any modifications to its value. You must specify an account name and key. Optionally,
        you can specify a label to lock a specific labeled version of the key-value.
        """;

    [McpServerTool(Destructive = false, ReadOnly = false)]
    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var args = BindArguments(parseResult);

        try
        {
            if (!await ProcessArguments(context, args))
            {
                return context.Response;
            }

            var appConfigService = context.GetService<IAppConfigService>();
            await appConfigService.LockKeyValue(
                args.Account!,
                args.Key!,
                args.Subscription!,
                args.Tenant,
                args.RetryPolicy,
                args.Label);

            context.Response.Results =
                ResponseResult.Create(
                    new KeyValueLockCommandResult(args.Key, args.Label),
                    AppConfigJsonContext.Default.KeyValueLockCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred locking value. Key: {Key}, Label: {Label}", args.Key, args.Label);
            HandleException(context.Response, ex);
        }

        return context.Response;
    }

    internal record KeyValueLockCommandResult(string? Key, string? Label);
}
