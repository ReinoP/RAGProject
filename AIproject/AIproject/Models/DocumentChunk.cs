using System.ComponentModel.DataAnnotations;

namespace AIproject.Models
{
   public class DocumentChunk
    {
        [Key]
        public int Id;
        public string UserId { get; set; }
        public string Content { get; set; }       
        public string Embedding { get; set; }     // serialized float[] (JSON)
        public string SourceFile { get; set; }    // optional, file name
        public int ChunkIndex { get; set; }       // order in the document
    }
}
