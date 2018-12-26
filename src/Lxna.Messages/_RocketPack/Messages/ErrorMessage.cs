using System.Text;

namespace Lxna.Messages
{
    public sealed partial class ErrorMessage 
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{nameof(this.Type)}: {this.Type}");
            sb.AppendLine($"{nameof(this.Message)}: {this.Message}");
            sb.AppendLine($"{nameof(this.StackTrace)}: {this.StackTrace}");

            return sb.ToString();
        }
    }
}
