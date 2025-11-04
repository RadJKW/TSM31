// Copyright (c) Microsoft.All rights reserved.
// Licensed under the MIT License.
namespace TSM31.Core.Models;

public record FunctionKey(
    string KeyText,
    string Label,
    Action OnKeyDown,
    bool IsEnabled = true);
