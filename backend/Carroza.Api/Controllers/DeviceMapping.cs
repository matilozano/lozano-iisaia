using Carroza.Api.Application.Services;
using Carroza.Api.Contracts.DTOs;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Components;

namespace Carroza.Api.Controllers;

internal static class DeviceMapping
{
    public static ComponentCommand ToCommand(this DeviceCommandRequest request) =>
        new(request.Action, request.Direction, request.Speed, request.Position);

    public static DeviceDto ToDto(this ComponentSnapshot item) =>
        new(item.Component.Id, item.Component.Name, item.Component.Type,
            item.State.State, item.State.Online, item.State.Direction, item.State.Speed, item.State.Channels, item.State.Effect, item.State.EffectSpeed, item.State.Position, item.State.Movement, item.State.LimitExtended, item.State.LimitRetracted);

    public static DeviceStateDto ToDto(this ComponentState state) =>
        new(state.Id, state.State, state.Online, state.Direction, state.Speed, state.Channels, state.Effect, state.EffectSpeed, state.Position, state.Movement, state.LimitExtended, state.LimitRetracted);

    public static DeviceResultDto ToDto(this ComponentResult result) =>
        new(result.ComponentId, result.Success, result.State.ToDto(), result.ExecutedAt);
}
