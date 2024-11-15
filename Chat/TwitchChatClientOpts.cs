using TwitchSimpleLib.Irc;

namespace TwitchSimpleLib.Chat;

public class TwitchChatClientOpts : IBaseClientOpts
{
    public string Username { get; set; }
    public string OauthToken { get; set; }

    /// <summary>
    /// Если этот объект создан без <see cref="Username"/> и <see cref="OauthToken"/>, будет выполнен анонимный вход
    /// </summary>
    public bool Anonymous { get; set; }

    public TimeSpan PingDelay { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan PingTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan MinReconnectTime { get; set; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan MaxReconnectTime { get; set; } = TimeSpan.FromSeconds(15);

    public TwitchChatClientOpts(string username, string oauthToken)
    {
        Username = username;
        OauthToken = oauthToken;
        Anonymous = false;
    }

    public TwitchChatClientOpts()
    {
        Username = TwitchChatClient.GenerateAnonymName();
        OauthToken = "SCHMOOPIIE";
        Anonymous = true;
    }
}