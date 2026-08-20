namespace SbmFizikusToMqtt.MqttConnector.Interfaces;

/// <summary>
///     Provides access to the MQTT broker connection state.
/// </summary>
public interface IMqttConnection
{
    /// <summary>
    ///     Waits until the MQTT client has connected to the broker.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    Task WaitUntilConnectedAsync(CancellationToken cancellationToken = default);
}