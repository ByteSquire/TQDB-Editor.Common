using Avalonia.Controls;
using TQDB_Parser.DBR;

namespace TQDB_Editor.Common.Controls
{
    public interface IVariableControl : IControl
    {
        public event Func<string>? Submitted;

        public void Init(DBREntry entry, DBRFile file);
    }
}
