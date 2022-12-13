using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TQDB_Parser.DBR;

namespace TQDB_Editor.Controls
{
    public abstract partial class VariableControl : Control
    {
        [Signal]
        public delegate void SubmittedEventHandler();

        public DBREntry Entry { get; set; }

        public DBRFile DBRFile { get; set; }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            TooltipText = Entry.Value;
            if (!Entry.IsValid())
            {
                var redTheme = new Theme();
                redTheme.SetThemeItem(Theme.DataType.Color, "font_color", "TooltipLabel", Colors.Red);
                Theme = redTheme;
            }
            foreach (var child in GetChildren())
                if (child is Control cChild)
                    cChild.TooltipText = TooltipText;
            InitVariable(Entry);
        }

        protected abstract void InitVariable(DBREntry entry);

        public abstract string GetChangedValue();

        protected void OnConfirmed()
        {
            EmitSignal(nameof(Submitted));
        }
    }
}
