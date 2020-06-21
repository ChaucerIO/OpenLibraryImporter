using System;
using System.Collections.Generic;

namespace Chaucer.Common.Catalog
{
    public abstract class Media
    {
        /// <summary>
        /// Usually an ISBN (books), ISAN (movies), etc.
        /// </summary>
        public string Id { get; set; }
        public string Title { get; set; }
        public IReadOnlyList<string> Authors { get; set; }
        public DateTime PublishDate { get; set; }
        public int Edition { get; set; }
        public decimal ReplacementCost { get; set; }
    }
}