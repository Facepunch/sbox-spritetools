using Sandbox.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools;

public class UndoOp
{
    public string name;
    public string undoBuffer;
    public string redoBuffer;
}

public class UndoStack
{
    private readonly List<UndoOp> _undoStack = new();
    private int _undoLevel = 0;
    private bool _redoPending = false;

    public bool CanUndo => _undoLevel != 0 && !_redoPending;
    public bool CanRedo => _undoLevel != _undoStack.Count && !_redoPending;

    public string UndoName => CanUndo ? $"Undo {_undoStack[_undoLevel - 1].name}" : null;
    public string RedoName => CanRedo ? $"Redo {_undoStack[_undoLevel].name}" : null;

    public int UndoLevel => _undoLevel;
    public UndoOp MostRecent => _undoStack.LastOrDefault();

    public IEnumerable<string> Names => _undoStack.Select(x => x.name);

    public void PushUndo(string name, string buffer)
    {
        Assert.False(_redoPending, $"Pending Redo ({UndoName})");

        _redoPending = true;

        if (_undoStack.Count > _undoLevel)
        {
            var count = _undoStack.Count - _undoLevel;
            _undoStack.RemoveRange(_undoStack.Count - count, count);
        }

        _undoStack.Add(new() { name = name, undoBuffer = buffer });
        _undoLevel++;
    }

    public void PushRedo(string buffer)
    {
        Assert.True(_redoPending);

        _redoPending = false;
        _undoStack[_undoLevel - 1].redoBuffer = buffer;
    }

    public UndoOp Undo()
    {
        if (_redoPending)
        {
            Log.Warning("Pending Redo!");
            return null;
        }

        if (_undoStack.Count > 0 && _undoLevel > 0)
        {
            _undoLevel--;

            return _undoStack[_undoLevel];
        }

        return null;
    }

    public UndoOp Redo()
    {
        if (_redoPending)
        {
            Log.Warning("Pending Redo!");
            return null;
        }

        if (_undoStack.Count > 0 && _undoLevel <= _undoStack.Count - 1)
        {
            _undoLevel++;

            return _undoStack[_undoLevel - 1];
        }

        return null;
    }

    public UndoOp SetUndoLevel(int undoLevel)
    {
        if (_redoPending)
        {
            Log.Warning("Pending Redo!");
            return null;
        }

        if (_undoLevel == undoLevel + 1)
            return null;

        if (_undoStack.Count > 0 && undoLevel <= _undoStack.Count - 1)
        {
            _undoLevel = undoLevel + 1;

            return _undoStack[_undoLevel - 1];
        }

        return null;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _undoLevel = 0;
        _redoPending = false;
    }

    public void PopMostRecent()
    {
        if (_undoStack.Count > 0)
        {
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _undoLevel = _undoStack.Count;
        }
    }
}
