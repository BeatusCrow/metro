using System.Collections.Generic;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server._Metro14.SponsorSystem.GhostSkinSystem;

[RegisterComponent]
public sealed partial class CanBeSponsorGhostComponent : Component
{
    /// <summary>
    /// Состояние спрайта по умолчанию для не-спонсоров  
    /// </summary>
    public string DefaultState = "ghost";

    /// <summary>
    /// Маппинг уровней спонсора на состояния спрайта  
    /// </summary>
    public Dictionary<string, string> SponsorStates = new()
    {
        { "soldier", "ghost_camo" },
        { "lieutenant", "ghost_fire" },
        { "colonel", "ghost_blazeit" },
        { "beatus_individual_tier", "ghostburger" },
        { "ramzesina_individual_tier", "god" },
        { "kompotik_individual_tier", "uncloak" }
    };

    /// <summary>
    /// Список уровней спонсора.
    /// Самый последниий - самый высокий.
    /// Данное решение необходимо для возможности обладателю, например, подписки полковника менять свой скин на солдата/лейтенанта/полковника.
    /// </summary>
    public List<string> SponsorsRankStates = new List<string>() {
        "ghost",
        "ghost_camo",
        "ghost_fire",
        "ghost_blazeit",
        "ghostburger",
        "god",
        "uncloak",
    };

    /// <summary>
    /// Это поле необходимо для переключения скинов. В нем хранятся все доступные игроку скины.
    /// </summary>
    public List<string> AvailableStates = new List<string>();

    /// <summary>
    /// Индекс текущего включенного скина.
    /// </summary>
    public int CurrentIndex = 0;

    /// <summary>
    /// Сущность, хранящая действие смены скина наблюдателя.
    /// </summary>
    public EntityUid? TryChangeGhostSkinActionEntity;

    /// <summary>
    /// ID прототипа действия смены скина наблюдателя.
    /// </summary>
    [DataField]
    public EntProtoId TryChangeGhostSkinAction = "ActionChangeGhostSkin";
}

/// <summary>
/// Событие, поднимаемое при нажатии кнопки-действия для переключения спрайта.
/// </summary>
public sealed partial class TryChangeGhostSkinActionEvent : InstantActionEvent { }

[Serializable, NetSerializable]
public enum GhostVisuals : byte
{
    SpriteState,
}

[Serializable, NetSerializable]
public enum GhostVisualLayers : byte
{
    Base,
}
