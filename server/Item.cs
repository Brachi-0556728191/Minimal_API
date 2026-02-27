using System;
using System.Collections.Generic;

namespace TodoApi;

public partial class Item
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public bool? IsComplete { get; set; }

    // [cite_start]
    // השדה החדש שמקשר למשתמש [cite: 3]
    public int UserId { get; set; }
}
