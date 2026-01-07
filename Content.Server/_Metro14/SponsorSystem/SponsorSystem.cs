using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Content.Server.Database;
using Robust.Shared.Network;
using System.Linq;

namespace Content.Server._Metro14.SponsorSystem;

/// <summary>
/// Класс-обработчик команды для добавления игрока в БД спонсоров.
/// </summary>
[AdminCommand(AdminFlags.Sponsor)]
public sealed class SponsorSystemAddCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "sposorsystem_add";
    public override string Description => Loc.GetString("cmd-sponsorsystem-add-desc");
    public override string Help => Loc.GetString("cmd-sponsorsystem-add-help");

    private List<string> _SubscriptionsTiers = new List<string>() {
        "soldier",
        "lieutenant",
        "colonel",
        "beatus_individual_tier",
        "ramzesina_individual_tier",
        "kompotik_individual_tier"
    };

    private List<string> _PrivateSubscriptionsTiers = new List<string>() {
        "beatus_individual_tier",
        "ramzesina_individual_tier",
        "kompotik_individual_tier"
    };

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2 || args.Length > 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            shell.WriteLine(Help);
            return;
        }

        if (!Guid.TryParse(args[0], out var playerId))
        {
            var player = _playerManager.Sessions
                .FirstOrDefault(s => s.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));

            if (player == null)
            {
                shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }
            playerId = player.UserId.UserId;
        }

        var tier = args[1];
        DateTime? expiryDate = null;

        if (!_SubscriptionsTiers.Contains(tier))
        {
            shell.WriteError(Loc.GetString("cmd-sponsorsystem-add-tier-does-not-exist"));
            return;
        }

        if (_PrivateSubscriptionsTiers.Contains(tier))
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteError("Команда может быть выполнена только игроком");
                return;
            }

            var sessionKey = player.UserId;
            shell.WriteLine($"Ваш сикей: {sessionKey}");
        }

        if (args.Length == 3 && int.TryParse(args[2], out var days))
        {
            expiryDate = DateTime.UtcNow.AddDays(days);
        }

        try
        {
            await _dbManager.AddOrUpdateSponsorAsync(
                new NetUserId(playerId),
                tier,
                expiryDate
            );

            shell.WriteLine(Loc.GetString("cmd-sponsorsystem-add-successfully", ("playerId", playerId), ("tier", tier)));
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-sponsorsystem-add-failed", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                "Player name or GUID"
            ),
            2 => CompletionResult.FromHintOptions(
                new CompletionOption[]
                {
                    new("soldier", "Soldier sponsor tier"),
                    new("lieutenant", "Lieutenant sponsor tier"),
                    new("colonel", "The colonel sponsor tier")
                },
                "Sponsor tier"
            ),
            3 => CompletionResult.FromHint("Expiry days (optional, leave empty for permanent)"),
            _ => CompletionResult.Empty
        };
    }
}

/// <summary>
/// Класс-обработчик команды удаления игрока из БД спонсоров.
/// </summary>
[AdminCommand(AdminFlags.Sponsor)]
public sealed class SponsorSystemRemoveCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "sposorsystem_remove";
    public override string Description => Loc.GetString("cmd-sponsorsystem-remove-desc");
    public override string Help => Loc.GetString("cmd-sponsorsystem-remove-help");

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var userId = new NetUserId(Guid.Parse(args[0]));
        try
        {
            await _dbManager.RemoveSponsorAsync(userId);
            shell.WriteLine(Loc.GetString("cmd-sponsorsystem-remove-successfully", ("userId", userId)));
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-sponsorsystem-remove-failed", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                "Player name or GUID"
            ),
            _ => CompletionResult.Empty
        };
    }
}

/// <summary>
/// Класс-обработчик команды просмотра данных о подписке игрока из БД спонсоров.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorSystemCheckCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "sposorsystem_check";
    public override string Description => Loc.GetString("cmd-sponsorsystem-check-desc");
    public override string Help => Loc.GetString("cmd-sponsorsystem-check-help");

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        var userId = new NetUserId(Guid.Parse(args[0]));
        var isSponsor = await _dbManager.IsSponsorAsync(userId);
        var info = await _dbManager.GetSponsorInfoAsync(userId);

        shell.WriteLine(Loc.GetString("cmd-sponsorsystem-check-status", ("isSponsor", isSponsor)));
        if (info != null)
        {
            var expiry = info.ExpiryDate?.ToString("yyyy-MM-dd") ?? "Permanent";
            shell.WriteLine(Loc.GetString("cmd-sponsorsystem-check-successfully", ("tier", info.Tier), ("expiryDat", expiry), ("isActive", info.IsActive)));
        }
        else
        {
            shell.WriteError(Loc.GetString("cmd-sponsorsystem-check-failed"));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                "Player name or GUID"
            ),
            _ => CompletionResult.Empty
        };
    }
}

/// <summary>
/// Класс-обработчик команды просмотра всех игроков, чьи сикеи находятся в БД спонсоров.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorSystemListCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "sposorsystem_list";
    public override string Description => Loc.GetString("cmd-sponsorsystem-list-desc");
    public override string Help => Loc.GetString("cmd-sponsorsystem-list-help");

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 0), ("currentAmount", args.Length)));
            return;
        }

        try
        {
            var sponsors = await _dbManager.GetAllSponsorsAsync();

            if (sponsors.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-sponsorsystem-list-zero-sponsors"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-sponsorsystem-list-total-sponsors", ("count", sponsors.Count)));

            foreach (var sponsor in sponsors)
            {
                var status = sponsor.IsActive ? "Active" : "Inactive";
                var expiry = sponsor.ExpiryDate?.ToString("yyyy-MM-dd") ?? "Permanent";

                shell.WriteLine(Loc.GetString("cmd-sponsorsystem-list-sponsor-information", ("userId", sponsor.UserId), ("tier", sponsor.Tier), ("active", status), ("expiry", expiry)));
            }
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-sponsorsystem-list-failed", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            0 => CompletionResult.FromHint("Lists all sponsors"),
            _ => CompletionResult.Empty
        };
    }
}
