using System;

namespace Promete;

/// <summary>
///     Promete エンジンがこのシーンを登録しないようにマークするための属性です。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class IgnoredSceneAttribute : Attribute;
