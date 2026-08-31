using System.Data;
using System.Xml.Linq;

namespace EventItemBagEditor;

internal sealed class MainForm : Form
{
    private readonly TreeView _tree = new() { Dock = DockStyle.Fill, HideSelection = false };
    private readonly TextBox _search = new() { PlaceholderText = "Buscar bag...", Dock = DockStyle.Top };
    private readonly Button _openFolder = new() { Text = "Abrir carpeta", Dock = DockStyle.Top, Height = 34 };
    private readonly Button _save = new() { Text = "Guardar", AutoSize = true };
    private readonly Button _reload = new() { Text = "Recargar", AutoSize = true };
    private readonly Button _validate = new() { Text = "Validar", AutoSize = true };
    private readonly Label _fileLabel = new() { AutoEllipsis = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _configGrid = NewGrid();
    private readonly DataGridView _basicGrid = NewGrid();
    private readonly DataGridView _dropsGrid = NewGrid();
    private readonly DataGridView _poolsGrid = NewGrid();
    private readonly RichTextBox _rawXml = new() { Dock = DockStyle.Fill, Font = new Font("Consolas", 10f), WordWrap = false, AcceptsTab = true };
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _statusLabel = new() { Text = "Listo" };
    private readonly ToolStripStatusLabel _modeLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleRight };

    private string? _rootFolder;
    private BagDocument? _current;
    private bool _loading;

    private static readonly string[] ConfigColumns =
    {
        "EventName", "DropZen", "ItemDropRate", "ItemDropCount", "SetItemDropRate", "ItemDropType", "SendFirework", "Coin1", "Coin2", "Coin3", "GensContribution"
    };

    private static readonly string[] BasicColumns =
    {
        "Type", "Index", "MinLevel", "MaxLevel", "Option1", "Option2", "Option3", "NewOption", "Time", "DropRate", "SocketOption", "SocketOptionRate", "Comment"
    };

    private static readonly string[] DropColumns =
    {
        "DropIndex", "DropRate", "Section", "Rate", "Money", "Option", "DW", "DK", "FE", "MG", "DL", "SU", "RF"
    };

    private static readonly string[] PoolColumns =
    {
        "Section", "Index", "Level", "Grade", "Option0", "Option1", "Option2", "Option3", "Option4", "Option5", "Option6", "Duration", "Comment"
    };

    public MainForm()
    {
        Text = "AVCore EventItemBag Editor";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 680);
        Size = new Size(1450, 860);
        Font = new Font("Segoe UI", 9f);

        BuildUi();
        WireEvents();
        ConfigureGrids();

        var auto = TryFindDefaultFolder();
        if (auto is not null) LoadFolder(auto);
    }

    private void BuildUi()
    {
        var main = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 290,
            FixedPanel = FixedPanel.Panel1
        };

        var left = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        var leftTitle = new Label
        {
            Text = "EventItemBag",
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        left.Controls.Add(_tree);
        left.Controls.Add(_search);
        left.Controls.Add(_openFolder);
        left.Controls.Add(leftTitle);
        main.Panel1.Controls.Add(left);

        var right = new Panel { Dock = DockStyle.Fill };
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            ColumnCount = 5,
            Padding = new Padding(8, 6, 8, 4)
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.Controls.Add(_save, 0, 0);
        toolbar.Controls.Add(_reload, 1, 0);
        toolbar.Controls.Add(_validate, 2, 0);
        toolbar.Controls.Add(_fileLabel, 4, 0);

        _tabs.TabPages.Add(CreateGridTab("Configuración", _configGrid, allowRows: false));
        _tabs.TabPages.Add(CreateGridTab("Items básicos", _basicGrid, allowRows: true));
        _tabs.TabPages.Add(CreateGridTab("Drops Ex", _dropsGrid, allowRows: true));
        _tabs.TabPages.Add(CreateGridTab("Pools Ex", _poolsGrid, allowRows: true));
        _tabs.TabPages.Add(new TabPage("XML") { Controls = { _rawXml } });

        _status.Items.Add(_statusLabel);
        _status.Items.Add(_modeLabel);

        right.Controls.Add(_tabs);
        right.Controls.Add(toolbar);
        right.Controls.Add(_status);
        main.Panel2.Controls.Add(right);
        Controls.Add(main);
    }

    private TabPage CreateGridTab(string title, DataGridView grid, bool allowRows)
    {
        var tab = new TabPage(title);
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
        panel.Controls.Add(grid);

        if (allowRows)
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 6, 0, 0)
            };
            var add = new Button { Text = "+ Agregar", AutoSize = true, Tag = grid };
            var duplicate = new Button { Text = "Duplicar", AutoSize = true, Tag = grid };
            var remove = new Button { Text = "Eliminar", AutoSize = true, Tag = grid };
            add.Click += AddRow;
            duplicate.Click += DuplicateRow;
            remove.Click += RemoveRows;
            bar.Controls.Add(add);
            bar.Controls.Add(duplicate);
            bar.Controls.Add(remove);
            panel.Controls.Add(bar);
        }

        tab.Controls.Add(panel);
        return tab;
    }

    private void WireEvents()
    {
        _openFolder.Click += (_, _) => SelectFolder();
        _search.TextChanged += (_, _) => RefreshTree();
        _tree.AfterSelect += (_, e) => { if (e.Node.Tag is string path) OpenFile(path); };
        _save.Click += (_, _) => SaveCurrent();
        _reload.Click += (_, _) => ReloadCurrent();
        _validate.Click += (_, _) => ValidateCurrent(showSuccess: true);
        FormClosing += (_, e) =>
        {
            if (!HasChanges()) return;
            var choice = MessageBox.Show("Hay cambios sin guardar. ¿Salir de todas formas?", "EventItemBag Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (choice == DialogResult.No) e.Cancel = true;
        };
    }

    private void ConfigureGrids()
    {
        ConfigureGrid(_configGrid, ConfigColumns);
        ConfigureGrid(_basicGrid, BasicColumns);
        ConfigureGrid(_dropsGrid, DropColumns);
        ConfigureGrid(_poolsGrid, PoolColumns);

        _configGrid.AllowUserToAddRows = false;
        _configGrid.AllowUserToDeleteRows = false;
        _configGrid.RowHeadersVisible = false;
        _configGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        _basicGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        _dropsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        _poolsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

        foreach (var grid in new[] { _configGrid, _basicGrid, _dropsGrid, _poolsGrid })
        {
            grid.CellValueChanged += (_, _) => MarkModified();
            grid.RowsAdded += (_, _) => { if (!_loading) MarkModified(); };
            grid.RowsRemoved += (_, _) => { if (!_loading) MarkModified(); };
        }
        _rawXml.TextChanged += (_, _) => { if (!_loading && _tabs.SelectedIndex == 4) MarkModified(); };
    }

    private static DataGridView NewGrid() => new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        MultiSelect = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
        BackgroundColor = SystemColors.Window,
        BorderStyle = BorderStyle.FixedSingle,
        AutoGenerateColumns = false
    };

    private static void ConfigureGrid(DataGridView grid, IEnumerable<string> names)
    {
        grid.Columns.Clear();
        foreach (var name in names)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = FriendlyName(name),
                SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = name == "Comment" ? 200 : 70
            };
            grid.Columns.Add(col);
        }
    }

    private static string FriendlyName(string name) => name switch
    {
        "DropIndex" => "Tier",
        "DropRate" => "Chance/Peso",
        "Section" => "Pool",
        "EventName" => "Nombre evento",
        "DropZen" => "Zen",
        "ItemDropRate" => "Chance item /100",
        "ItemDropCount" => "Cantidad",
        "SetItemDropRate" => "Set chance /10000",
        "SendFirework" => "Fuegos",
        "GensContribution" => "Gens",
        "MinLevel" => "Nivel mín.",
        "MaxLevel" => "Nivel máx.",
        "NewOption" => "Excellent",
        "SocketOption" => "Sockets fijos",
        "SocketOptionRate" => "Tabla sockets",
        "Duration" => "Duración",
        "Comment" => "Nombre / comentario",
        _ => name
    };

    private void SelectFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecciona la carpeta EventItemBag",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
            InitialDirectory = _rootFolder ?? AppContext.BaseDirectory
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) LoadFolder(dialog.SelectedPath);
    }

    private string? TryFindDefaultFolder()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "EventItemBag"),
            Path.Combine(AppContext.BaseDirectory, "Data", "EventItemBag"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "Data", "EventItemBag"))
        };
        return candidates.FirstOrDefault(Directory.Exists);
    }

    private void LoadFolder(string folder)
    {
        _rootFolder = folder;
        Text = $"AVCore EventItemBag Editor — {folder}";
        RefreshTree();
        SetStatus($"Carpeta cargada: {folder}");
    }

    private void RefreshTree()
    {
        if (string.IsNullOrWhiteSpace(_rootFolder) || !Directory.Exists(_rootFolder)) return;

        var filter = _search.Text.Trim();
        var files = Directory.EnumerateFiles(_rootFolder, "*.xml", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}_Examples{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(f => string.IsNullOrWhiteSpace(filter) || Path.GetFileNameWithoutExtension(f).Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _tree.BeginUpdate();
        _tree.Nodes.Clear();
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(_rootFolder, file);
            var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            TreeNodeCollection current = _tree.Nodes;
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                TreeNode? node = current.Cast<TreeNode>().FirstOrDefault(n => string.Equals(n.Text, part, StringComparison.OrdinalIgnoreCase));
                if (node is null)
                {
                    node = new TreeNode(i == parts.Length - 1 ? Path.GetFileNameWithoutExtension(part) : part);
                    current.Add(node);
                }
                if (i == parts.Length - 1) node.Tag = file;
                current = node.Nodes;
            }
        }
        _tree.EndUpdate();
        if (!string.IsNullOrWhiteSpace(filter)) _tree.ExpandAll();
    }

    private void OpenFile(string path)
    {
        if (_current is not null && HasChanges())
        {
            var choice = MessageBox.Show("Hay cambios sin guardar. ¿Descartarlos y abrir otro archivo?", "EventItemBag Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (choice == DialogResult.No) return;
        }

        try
        {
            _loading = true;
            _current = BagDocument.Load(path);
            PopulateConfig(_current.GetConfig());
            PopulateRows(_basicGrid, _current.GetBasicItems());
            PopulateRows(_dropsGrid, _current.GetExtendedDrops());
            PopulateRows(_poolsGrid, _current.GetExtendedPoolItems());
            _rawXml.Text = _current.ToXmlString();
            _fileLabel.Text = path;
            _modeLabel.Text = _current.IsExtended ? "Formato: EXTENDIDO (UseEx=1)" : "Formato: BÁSICO (UseEx=0)";
            _tabs.TabPages[1].Enabled = !_current.IsExtended;
            _tabs.TabPages[2].Enabled = _current.IsExtended;
            _tabs.TabPages[3].Enabled = _current.IsExtended;
            _tabs.SelectedIndex = _current.IsExtended ? 2 : 1;
            SetModified(false);
            SetStatus($"Abierto: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "No se pudo abrir el XML", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void PopulateConfig(IReadOnlyDictionary<string, string> values)
    {
        _configGrid.Rows.Clear();
        var index = _configGrid.Rows.Add();
        foreach (DataGridViewColumn col in _configGrid.Columns)
            _configGrid.Rows[index].Cells[col.Name].Value = values.TryGetValue(col.Name, out var value) ? value : DefaultConfigValue(col.Name);
    }

    private static string DefaultConfigValue(string name) => name switch
    {
        "EventName" => "EventItemBag",
        "ItemDropRate" => "100",
        "ItemDropCount" => "1",
        _ => "0"
    };

    private static void PopulateRows(DataGridView grid, IEnumerable<Dictionary<string, string>> rows)
    {
        grid.Rows.Clear();
        foreach (var row in rows)
        {
            var index = grid.Rows.Add();
            foreach (DataGridViewColumn col in grid.Columns)
                if (row.TryGetValue(col.Name, out var value)) grid.Rows[index].Cells[col.Name].Value = value;
        }
    }

    private Dictionary<string, string> ReadConfig()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (_configGrid.Rows.Count == 0) return result;
        foreach (DataGridViewColumn col in _configGrid.Columns)
            result[col.Name] = CellText(_configGrid.Rows[0].Cells[col.Name]);
        return result;
    }

    private static List<IReadOnlyDictionary<string, string>> ReadRows(DataGridView grid)
    {
        var result = new List<IReadOnlyDictionary<string, string>>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var hasValue = false;
            foreach (DataGridViewColumn col in grid.Columns)
            {
                var value = CellText(row.Cells[col.Name]);
                dict[col.Name] = value;
                if (!string.IsNullOrWhiteSpace(value)) hasValue = true;
            }
            if (hasValue) result.Add(dict);
        }
        return result;
    }

    private static string CellText(DataGridViewCell cell) => Convert.ToString(cell.Value)?.Trim() ?? string.Empty;

    private void SaveCurrent()
    {
        if (_current is null) return;

        try
        {
            if (_tabs.SelectedIndex == 4)
            {
                _current.Save(_rawXml.Text);
                _current = BagDocument.Load(_current.FilePath);
            }
            else
            {
                _current.SetConfig(ReadConfig());
                if (_current.IsExtended)
                    _current.SetExtended(ReadRows(_dropsGrid), ReadRows(_poolsGrid));
                else
                    _current.SetBasicItems(ReadRows(_basicGrid));

                var errors = _current.Validate();
                if (errors.Count > 0)
                {
                    MessageBox.Show(this, string.Join(Environment.NewLine, errors.Take(30)), "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SetStatus($"No guardado: {errors.Count} problema(s)");
                    return;
                }

                _current.Save();
            }

            _loading = true;
            _rawXml.Text = _current.ToXmlString();
            _loading = false;
            SetModified(false);
            SetStatus($"Guardado: {Path.GetFileName(_current.FilePath)} — backup .bak creado");
        }
        catch (Exception ex)
        {
            _loading = false;
            MessageBox.Show(this, ex.Message, "Error al guardar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReloadCurrent()
    {
        if (_current is null) return;
        var path = _current.FilePath;
        SetModified(false);
        OpenFile(path);
    }

    private bool ValidateCurrent(bool showSuccess)
    {
        if (_current is null) return false;
        try
        {
            if (_tabs.SelectedIndex == 4)
            {
                var doc = XDocument.Parse(_rawXml.Text);
                if (doc.Root?.Name.LocalName != "ItemBag") throw new InvalidDataException("La raíz debe ser <ItemBag>.");
                if (showSuccess) MessageBox.Show(this, "XML bien formado.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }

            _current.SetConfig(ReadConfig());
            if (_current.IsExtended) _current.SetExtended(ReadRows(_dropsGrid), ReadRows(_poolsGrid));
            else _current.SetBasicItems(ReadRows(_basicGrid));
            var errors = _current.Validate();
            if (errors.Count == 0)
            {
                if (showSuccess) MessageBox.Show(this, "Configuración válida. No se detectaron errores estructurales.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("Validación OK");
                return true;
            }

            MessageBox.Show(this, string.Join(Environment.NewLine, errors.Take(40)), $"{errors.Count} problema(s)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            SetStatus($"Validación: {errors.Count} problema(s)");
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void AddRow(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: DataGridView grid }) return;
        var index = grid.Rows.Add();
        ApplyDefaults(grid, grid.Rows[index]);
        grid.CurrentCell = grid.Rows[index].Cells[0];
        grid.BeginEdit(true);
    }

    private static void ApplyDefaults(DataGridView grid, DataGridViewRow row)
    {
        foreach (DataGridViewColumn col in grid.Columns)
        {
            row.Cells[col.Name].Value = col.Name switch
            {
                "DropRate" when grid.Columns.Contains("DropIndex") => "10000",
                "Rate" => "10000",
                "DW" or "DK" or "FE" or "MG" or "DL" or "SU" or "RF" => "1",
                "Option0" or "Option1" or "Option2" or "Option3" or "Option4" or "Option5" or "Option6" when grid.Columns.Contains("Grade") => "-1",
                "Comment" => string.Empty,
                _ => "0"
            };
        }
    }

    private void DuplicateRow(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: DataGridView grid } || grid.CurrentRow is null) return;
        var source = grid.CurrentRow;
        var index = grid.Rows.Add();
        foreach (DataGridViewColumn col in grid.Columns)
            grid.Rows[index].Cells[col.Name].Value = source.Cells[col.Name].Value;
        grid.CurrentCell = grid.Rows[index].Cells[0];
    }

    private void RemoveRows(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: DataGridView grid }) return;
        var selected = grid.SelectedRows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).OrderByDescending(r => r.Index).ToList();
        if (selected.Count == 0 && grid.CurrentRow is { IsNewRow: false } current) selected.Add(current);
        if (selected.Count == 0) return;
        if (MessageBox.Show(this, $"¿Eliminar {selected.Count} fila(s)?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        foreach (var row in selected) grid.Rows.Remove(row);
    }

    private bool _modified;
    private void MarkModified() => SetModified(true);
    private bool HasChanges() => _modified;

    private void SetModified(bool value)
    {
        _modified = value;
        _save.Enabled = _current is not null && value;
        var marker = value ? " *" : string.Empty;
        if (_current is not null) _fileLabel.Text = _current.FilePath + marker;
    }

    private void SetStatus(string text) => _statusLabel.Text = text;
}
