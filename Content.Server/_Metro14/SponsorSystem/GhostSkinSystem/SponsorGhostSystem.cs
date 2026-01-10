using Content.Server.Database;
using Content.Server._Metro14.SponsorSystem.GhostSkinSystem;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Asynchronous;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Metro14.SponsorSystem.GhostSkinSystem;

/// <summary>
/// Класс с логикой смены скинов у наблюдателей спонсоров.
/// </summary>
public sealed class SponsorGhostSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public override void Initialize()
    {
        // инициализация и удаление компонента
        SubscribeLocalEvent<CanBeSponsorGhostComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CanBeSponsorGhostComponent, ComponentRemove>(OnComponentRemove);

        // выход в госта
        SubscribeLocalEvent<CanBeSponsorGhostComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<CanBeSponsorGhostComponent, PlayerAttachedEvent>(OnPlayerAttached);

        // нажатие кнопки-действия
        SubscribeLocalEvent<CanBeSponsorGhostComponent, TryChangeGhostSkinActionEvent>(OnTryChangeGhostSkinActionPressed);
    }

    /// <summary>
    /// Базовый метод обработки события инициализации компонента CanBeSponsorGhostComponent.
    /// </summary>
    private void OnComponentInit(EntityUid uid, CanBeSponsorGhostComponent component, ComponentInit args)
    {
        TrySetAction(uid, component.TryChangeGhostSkinAction, ref component.TryChangeGhostSkinActionEntity);
    }

    /// <summary>
    /// Базовый метод обработки события удаления компонента CanBeSponsorGhostComponent.
    /// </summary>
    private void OnComponentRemove(EntityUid uid, CanBeSponsorGhostComponent component, ComponentRemove args)
    {
        if (!TryComp<ActionsComponent>(uid, out var actionsComp))
            return;

        if (component.TryChangeGhostSkinActionEntity != null)
            _actionsSystem.RemoveAction((uid, actionsComp), component.TryChangeGhostSkinActionEntity);
    }

    /// <summary>
    /// Когда игрок переходит в призрака (смерть или /ghost).
    /// </summary>
    private async void OnMindAdded(EntityUid uid, CanBeSponsorGhostComponent component, MindAddedMessage args)
    {
        // защита от дураков, которые решат выдать данный компонент другим сущностям.
        if (!TryComp<GhostComponent>(uid, out var ghost))
            return;

        if (!TryComp<MindContainerComponent>(uid, out var mindContainer))
            return;

        if (mindContainer.Mind == null)
            return;

        var mindId = mindContainer.Mind.Value;

        if (!TryComp<MindComponent>(mindId, out var mind) || mind.UserId == null)
            return;

        var userId = mind.UserId.Value;

        var isSponsor = await _dbManager.IsSponsorAsync(userId);

        if (!isSponsor)
        {
            SetGhostSprite(uid, component.DefaultState);
            return;
        }

        var sponsorInfo = await _dbManager.GetSponsorInfoAsync(userId);

        if (sponsorInfo == null || !sponsorInfo.IsActive)
        {
            SetGhostSprite(uid, component.DefaultState);
            return;
        }

        var spriteState = component.SponsorStates.GetValueOrDefault(
            sponsorInfo.Tier.ToLower(),
            component.DefaultState
        );

        int indexSprite = component.SponsorsRankStates.IndexOf(spriteState);
        if (indexSprite >= 0)
        {
            component.CurrentIndex = indexSprite;
            for (int i = 0; i <= indexSprite; i++)
            {
                component.AvailableStates.Add(component.SponsorsRankStates[i]);
            }
        }

        SetGhostSprite(uid, spriteState);
    }

    /// <summary>
    /// Когда игрок переходит в админ призрака (/aghost).
    /// </summary>
    private async void OnPlayerAttached(EntityUid uid, CanBeSponsorGhostComponent component, PlayerAttachedEvent args)
    {
        // Проверяем, что присоединенная сущность - призрак с нашим компонентом  
        if (args.Entity != uid || !HasComp<GhostComponent>(uid))
            return;

        if (!TryComp<VisitingMindComponent>(uid, out var mindContainer))
            return;

        if (mindContainer.MindId == null)
            return;

        var mindId = mindContainer.MindId.Value;

        if (!TryComp<MindComponent>(mindId, out var mind) || mind.UserId == null)
            return;

        var userId = mind.UserId.Value;

        var isSponsor = await _dbManager.IsSponsorAsync(userId);

        if (!isSponsor)
        {
            SetGhostSprite(uid, component.DefaultState);
            return;
        }

        var sponsorInfo = await _dbManager.GetSponsorInfoAsync(userId);

        if (sponsorInfo == null || !sponsorInfo.IsActive)
        {
            SetGhostSprite(uid, component.DefaultState);
            return;
        }

        var spriteState = component.SponsorStates.GetValueOrDefault(
            sponsorInfo.Tier.ToLower(),
            component.DefaultState
        );

        int indexSprite = component.SponsorsRankStates.IndexOf(spriteState);
        if (indexSprite >= 0)
        {
            component.CurrentIndex = indexSprite;
            for (int i = 0; i <= indexSprite; i++)
            {
                component.AvailableStates.Add(component.SponsorsRankStates[i]);
            }
        }

        SetGhostSprite(uid, spriteState);
    }

    /// <summary>
    /// Вспомогательный метод для установки нужного спрайта призраку.
    /// </summary>
    private void SetGhostSprite(EntityUid uid, string spriteState)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.SetData(uid, GhostVisuals.SpriteState, spriteState, appearance);
    }

    /// <summary>
    /// Обработчик ивента, поднимаемого при нажатии кнопки для смены скина наблюдателя.
    /// </summary>
    private void OnTryChangeGhostSkinActionPressed(EntityUid uid, CanBeSponsorGhostComponent component, TryChangeGhostSkinActionEvent args)
    {
        if (!TryComp<GhostComponent>(uid, out var ghost))
            return;

        if (component.TryChangeGhostSkinActionEntity == null)
            return;

        component.CurrentIndex = ChangeIndex(component.CurrentIndex, component.AvailableStates.Count);
        var spriteState = component.SponsorsRankStates[component.CurrentIndex];

        SetGhostSprite(uid, spriteState);

        _actionsSystem.StartUseDelay(component.TryChangeGhostSkinActionEntity.Value);
    }

    /// <summary>
    /// Вспомогательный метод для безопасной смены индекса скина.
    /// </summary>
    private int ChangeIndex(int currentIndex, int maxIndex)
    {
        return currentIndex + 1 >= maxIndex ? 0 : currentIndex + 1;
    }

    /// <summary>
    /// Вспомогательный метод для установки действия смены скина наблюдателя.
    /// </summary>
    private void TrySetAction(EntityUid uid, EntProtoId actionProtoId, ref EntityUid? actionEntityUid)
    {
        actionEntityUid = _actionsSystem.AddAction(uid, actionProtoId);

        if (actionEntityUid != null)
        {
            _actionsSystem.StartUseDelay(actionEntityUid.Value);
        }
    }
}
