using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Idlebound.Foundation.Development
{
    // Presentation identities only. No domain boss definitions, kills, unlocks or ownership.
    internal static class BossCollectionShell
    {
        internal sealed class Item { internal string Name, IconKey; }
        internal sealed class Collection { internal string Boss; internal ReadOnlyCollection<Item> Items; }
        internal static ReadOnlyCollection<Collection> All
        {
            get
            {
                var data = Resources.Load<TextAsset>("Idlebound/CollectionPreview");
                if (data == null) throw new InvalidOperationException("Collection preview missing.");
                var rows = data.text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")).Select(l => l.Split('|')).ToArray();
                if (rows.Any(r => r.Length != 4 || r[3] != "unimplemented")) throw new InvalidOperationException("Invalid collection shell data.");
                return rows.GroupBy(r => r[0]).Select(g => new Collection { Boss = g.Key,
                    Items = g.Select(r => new Item { Name = r[1], IconKey = r[2] }).ToList().AsReadOnly() }).ToList().AsReadOnly();
            }
        }
    }
}
