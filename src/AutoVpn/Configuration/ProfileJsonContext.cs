using System.Text.Json.Serialization;

namespace AutoVpn.Configuration;

[JsonSerializable(typeof(Profile))]
internal sealed partial class ProfileJsonContext : JsonSerializerContext;
