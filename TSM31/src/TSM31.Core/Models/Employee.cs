// Copyright (c) Microsoft.All rights reserved.
// Licensed under the MIT License.
namespace TSM31.Core.Models;

public class Employee
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public string Initials
    {
        get {
            var nameParts = Name.Split(' ');
            return nameParts.Length switch {
                3 => $"{nameParts[1][0]}{nameParts[2][0]}{nameParts[0][0]}",
                2 => $"{nameParts[1][0]}{nameParts[0][0]}",
                _ => string.Empty
            };
        }
    }

    public string SuperVisorId { get; set; } = string.Empty;

    public bool IsValid { get; init; }

}
