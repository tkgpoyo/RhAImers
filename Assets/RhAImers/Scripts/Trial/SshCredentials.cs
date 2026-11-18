using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using UnityEngine;

/// <summary>
/// SSH 接続情報。アセットは Assets/RhAImers/Local/ に置くこと（.gitignore 済みでコミットされない）。
/// SSH.NET は ~/.ssh/config を読まないので、HostName / User / IdentityFile の値をここに直接書く。
/// </summary>
[CreateAssetMenu(fileName = "SshCredentials", menuName = "RhAImers/Local/SSH Credentials")]
public class SshCredentials : ScriptableObject
{
    [SerializeField, Tooltip("IP アドレスまたはホスト名（~/.ssh/config の Host 別名は使えない）")]
    private string _host;
    [SerializeField]
    private int _port = 22;
    [SerializeField]
    private string _userName;

    [Header("公開鍵認証")]
    [SerializeField, Tooltip("秘密鍵のパス。先頭の ~ はホームディレクトリに展開される")]
    private string _privateKeyPath = "~/.ssh/id_ed25519";
    [SerializeField, Tooltip("秘密鍵にパスフレーズがある場合のみ")]
    private string _privateKeyPassphrase;

    [Header("パスワード認証（任意）")]
    [SerializeField]
    private string _password;

    public string Host => _host;
    public int Port => _port;
    public string UserName => _userName;

    public ConnectionInfo CreateConnectionInfo(TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(_host)) throw new InvalidOperationException($"{name}: Host が未設定です");
        if (string.IsNullOrWhiteSpace(_userName)) throw new InvalidOperationException($"{name}: UserName が未設定です");
        var connectHost = ValidateHost(_host.Trim());

        var methods = new List<AuthenticationMethod>();

        if (!string.IsNullOrWhiteSpace(_privateKeyPath))
        {
            var path = ExpandHome(_privateKeyPath);
            if (!File.Exists(path)) throw new FileNotFoundException($"{name}: 秘密鍵が見つかりません", path);

            var keyFile = string.IsNullOrEmpty(_privateKeyPassphrase)
                ? new PrivateKeyFile(path)
                : new PrivateKeyFile(path, _privateKeyPassphrase);
            methods.Add(new PrivateKeyAuthenticationMethod(_userName, keyFile));
        }

        if (!string.IsNullOrEmpty(_password))
        {
            methods.Add(new PasswordAuthenticationMethod(_userName, _password));
        }

        if (methods.Count == 0) throw new InvalidOperationException($"{name}: 秘密鍵かパスワードを設定してください");

        return new ConnectionInfo(connectHost, _port, _userName, methods.ToArray())
        {
            Timeout = timeout,
        };
    }

    private string ValidateHost(string host)
    {
        if (System.Net.IPAddress.TryParse(host, out _)) return host;

        try
        {
            System.Net.Dns.GetHostAddresses(host);
        }
        catch (System.Net.Sockets.SocketException e)
        {
            throw new InvalidOperationException(
                $"{name}: Host '{host}' を名前解決できません。~/.ssh/config の Host 別名は使えないので、" +
                "HostName の値（IP アドレスなど）を入れてください", e);
        }

        return host;
    }

    private static string ExpandHome(string path)
    {
        if (path == "~" || path.StartsWith("~/"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path.Length > 2 ? path.Substring(2) : string.Empty);
        }

        return path;
    }
}
