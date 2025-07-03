using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GraphicAlgorithms2
{
    internal class FrmGraphicAlgorithms : Form
    {
        // Campos de clase para acceso global
        private GroupBox grbParams;
        private TabControl tabAlgorithms;
        private PictureBox picCanvas;
        private ToolStripStatusLabel statusMouse;
        private ToolStripStatusLabel statusModo;
        private ToolStripStatusLabel statusEstado;
        private ToolStripStatusLabel statusPuntos;
        private int previousTabIndex = 0;

        // Botones y controles de ejecución
        private Button btnCompleto;
        private Button btnPausar;
        private Button btnReset;
        private Label lblInfo;
        private TrackBar sldSpeed;

        // Variables de estado
        private Bitmap canvasBitmap;
        private CancellationTokenSource cancellationTokenSource;
        private bool isExecuting = false;
        private string selectedAlgorithm = "";
        private List<Point> clipLinePoints = new List<Point>();
        private Polygon clipPolygon = new Polygon();
        private Clipper clipper;
        private bool showingClippingArea = false;
        private bool isCreatingPolygon = false;

        private Curve curve = new Curve();
        private bool isCreatingCurve = false;

        public FrmGraphicAlgorithms()
        {
            InitializeComponent();
            var defaultClipArea = new Rectangle(150, 100, 300, 200);
            clipper = new Clipper(defaultClipArea);
        }

        #region UI Helper Methods

        private Button CreateColorButton(string text, Color defaultColor, int top, int left, int width)
        {
            var button = new Button
            {
                Text = text,
                Top = top,
                Left = left,
                Width = width,
                BackColor = defaultColor,
                ForeColor = defaultColor.GetBrightness() > 0.5 ? Color.Black : Color.White
            };

            button.Click += (s, e) =>
            {
                var cd = new ColorDialog();
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    button.BackColor = cd.Color;
                    button.ForeColor = cd.Color.GetBrightness() > 0.5 ? Color.Black : Color.White;
                }
            };

            return button;
        }

        private TextBox CreateNamedTextBox(string name, int left, int top, int width, string defaultValue = "")
        {
            return new TextBox { Name = name, Left = left, Top = top, Width = width, Text = defaultValue };
        }

        private Label CreateLabel(string text, int top, int left, int width = 110, Color? foreColor = null)
        {
            var label = new Label { Text = text, Top = top, Left = left, Width = width };
            if (foreColor.HasValue)
                label.ForeColor = foreColor.Value;
            return label;
        }

        private FlowLayoutPanel CreateRadioGroup(string[] options)
        {
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
            foreach (var opt in options)
            {
                var rb = new RadioButton { Text = opt, AutoSize = true };
                rb.CheckedChanged += (s, e) =>
                {
                    if (rb.Checked && !isExecuting)
                    {
                        selectedAlgorithm = rb.Text;
                        UpdateInfoForCurrentTab();
                    }
                };
                panel.Controls.Add(rb);
            }
            if (panel.Controls.Count > 0)
            {
                var firstRadio = (RadioButton)panel.Controls[0];
                firstRadio.Checked = true;
                selectedAlgorithm = firstRadio.Text;
            }
            return panel;
        }

        #endregion

        #region Control Finder Methods

        private T FindControlByName<T>(string name) where T : Control
        {
            return grbParams.Controls.OfType<T>().FirstOrDefault(c => c.Name == name);
        }

        private T FindControlByText<T>(string text) where T : Control
        {
            return grbParams.Controls.OfType<T>().FirstOrDefault(c => c.Text == text);
        }

        private Dictionary<string, TextBox> GetTextBoxes(params string[] names)
        {
            var result = new Dictionary<string, TextBox>();
            foreach (var name in names)
            {
                var textBox = FindControlByName<TextBox>(name);
                if (textBox != null)
                    result[name] = textBox;
            }
            return result;
        }

        #endregion

        #region Validation Methods

        private (bool success, int[] values, string error) ValidateCoordinates(Dictionary<string, TextBox> textBoxes)
        {
            var values = new List<int>();

            foreach (var kvp in textBoxes)
            {
                if (!int.TryParse(kvp.Value.Text, out int value))
                {
                    return (false, null, $"Valor inválido en {kvp.Key}");
                }
                values.Add(value);
            }

            return (true, values.ToArray(), null);
        }

        private (bool success, string error) ValidateCanvasBounds(int[] coordinates)
        {
            for (int i = 0; i < coordinates.Length; i += 2)
            {
                if (i + 1 >= coordinates.Length) break;

                int x = coordinates[i];
                int y = coordinates[i + 1];

                if (x < 0 || x >= canvasBitmap.Width || y < 0 || y >= canvasBitmap.Height)
                {
                    return (false, $"Coordenadas ({x},{y}) fuera del canvas (0-{canvasBitmap.Width}, 0-{canvasBitmap.Height})");
                }
            }
            return (true, null);
        }

        private Color GetButtonColor(Button colorButton, Color defaultColor)
        {
            if (colorButton?.BackColor == null || colorButton.BackColor == SystemColors.Control)
                return defaultColor;
            return colorButton.BackColor;
        }

        #endregion



        #region UI Information Methods

        private void UpdateExecutionInfo(string algorithmName, string state, string details)
        {
            lblInfo.Text = $"Algoritmo: {algorithmName}\nEstado: {state}\n{details}";
        }

        private void ShowError(string message)
        {
            MessageBox.Show($"Error: {message}");
        }

        #endregion

        #region Parameter Panel Builders

        private void BuildLineParameterPanel()
        {
            var controls = new Control[]
            {
                    CreateLabel("Punto Inicial (X1, Y1):", 20, 10),
                    CreateNamedTextBox("txtX1", 150, 20, 50, "100"),
                    CreateNamedTextBox("txtY1", 210, 20, 50, "100"),
                    CreateLabel("Punto Final (X2, Y2):", 50, 10),
                    CreateNamedTextBox("txtX2", 150, 50, 50, "400"),
                    CreateNamedTextBox("txtY2", 210, 50, 50, "400"),
                    CreateColorButton("Color de Línea", Color.Black, 90, 10, 250)
            };

            grbParams.Controls.AddRange(controls);
        }

        private void BuildShapeParameterPanel()
        {
            var controls = new Control[]
            {
                    CreateLabel("Centro (X, Y):", 20, 10),
                    CreateNamedTextBox("txtCenterX", 150, 20, 50, "300"),
                    CreateNamedTextBox("txtCenterY", 210, 20, 50, "300"),
                    CreateLabel("Radio:", 50, 10),
                    CreateNamedTextBox("txtRadius", 150, 50, 50, "50"),
                    CreateLabel("Semi-ejes (A, B):", 80, 10),
                    CreateNamedTextBox("txtSemiA", 150, 80, 50, "80"),
                    CreateNamedTextBox("txtSemiB", 210, 80, 50, "60"),
                    CreateColorButton("Color de Figura", Color.Black, 110, 10, 250)
            };

            grbParams.Controls.AddRange(controls);
        }

        private void BuildFillParameterPanel()
        {
            var controls = new Control[]
            {
                    CreateLabel("Semilla (X, Y):", 20, 10),
                    CreateNamedTextBox("txtSeedX", 150, 20, 50),
                    CreateNamedTextBox("txtSeedY", 210, 20, 50),
                    CreateLabel("Nota: Click en canvas para seleccionar", 50, 10, 250, Color.Gray),
                    CreateColorButton("Color de Relleno", Color.Red, 80, 10, 250)
            };

            grbParams.Controls.AddRange(controls);
        }

        private void BuildClipParameterPanel()
        {
            var controls = new Control[]
            {
                CreateLabel("Área de Recorte:", 20, 10),
                CreateLabel("X1, Y1, X2, Y2:", 45, 10),
                CreateNamedTextBox("txtClipX1", 120, 40, 40, clipper.GetClippingArea().X.ToString()),
                CreateNamedTextBox("txtClipY1", 170, 40, 40, clipper.GetClippingArea().Y.ToString()),
                CreateNamedTextBox("txtClipX2", 220, 40, 40, clipper.GetClippingArea().Right.ToString()),
                CreateNamedTextBox("txtClipY2", 270, 40, 40, clipper.GetClippingArea().Bottom.ToString()),
                CreateLabel(GetClipInstructions(), 70, 10, 280, Color.Gray),
                CreateColorButton("Color de Línea", Color.Blue, 100, 10, 90),
                CreateColorButton("Color de Área", Color.Red, 100, 110, 90)
            };

            grbParams.Controls.AddRange(controls);

            // Solo mostrar área de recorte por defecto si es Sutherland-Hodgman
            if (selectedAlgorithm.Contains("Sutherland-Hodgman"))
                ShowDefaultClippingArea();
        }


        private string GetClipInstructions()
        {
            if (selectedAlgorithm.Contains("Cohen-Sutherland"))
            {
                return "Cohen-Sutherland:\nClick 2 puntos para línea";
            }
            else if (selectedAlgorithm.Contains("Sutherland-Hodgman"))
            {
                return "Sutherland-Hodgman:\nClick puntos del polígono\nSe cierra automáticamente";
            }
            return "Seleccione un algoritmo";
        }

        private void BuildCurveParameterPanel()
        {
            var controls = new Control[]
            {
                CreateLabel("Puntos de Control:", 20, 10),
                CreateLabel("Click en canvas para agregar puntos", 45, 10, 280, Color.Gray),
                CreateLabel("Mínimo 2 puntos para Bézier", 70, 10, 280, Color.Gray),
                CreateLabel("Mínimo 4 puntos para B-Spline", 95, 10, 280, Color.Gray),
                CreateColorButton("Color de Curva", Color.Purple, 125, 10, 250)
            };

            grbParams.Controls.AddRange(controls);
        }

        #endregion

        #region Algorithm Execution Methods

        private async Task ExecuteLineAlgorithmComplete()
        {
            try
            {
                var textBoxes = GetTextBoxes("txtX1", "txtY1", "txtX2", "txtY2");
                var colorButton = FindControlByText<Button>("Color de Línea");

                if (textBoxes.Count != 4)
                {
                    ShowError("No se pudieron encontrar todos los controles de entrada.");
                    return;
                }

                var (success, coordinates, error) = ValidateCoordinates(textBoxes);
                if (!success)
                {
                    ShowError(error);
                    return;
                }

                var (boundsValid, boundsError) = ValidateCanvasBounds(coordinates);
                if (!boundsValid)
                {
                    ShowError(boundsError);
                    return;
                }

                var selectedColor = GetButtonColor(colorButton, Color.Black);
                var line = new Line(new Point(coordinates[0], coordinates[1]), new Point(coordinates[2], coordinates[3]));

                UpdateExecutionInfo(selectedAlgorithm, "Ejecutando", $"Línea: ({coordinates[0]},{coordinates[1]}) -> ({coordinates[2]},{coordinates[3]})");

                if (string.IsNullOrEmpty(selectedAlgorithm) || selectedAlgorithm.Contains("DDA"))
                {
                    await line.DrawDDA(picCanvas, canvasBitmap, selectedColor, true, cancellationTokenSource.Token);
                }
                else if (selectedAlgorithm.Contains("Bresenham"))
                {
                    await line.DrawBresenham(picCanvas, canvasBitmap, selectedColor, true, cancellationTokenSource.Token);
                }

                UpdateExecutionInfo(selectedAlgorithm, "Completado", $"Línea: ({coordinates[0]},{coordinates[1]}) -> ({coordinates[2]},{coordinates[3]})");
            }
            catch (Exception ex)
            {
                ShowError($"Error al ejecutar el algoritmo: {ex.Message}");
                UpdateExecutionInfo(selectedAlgorithm, "Error", ex.Message);
            }
        }

        private async Task ExecuteShapeAlgorithmComplete()
        {
            try
            {
                var centerTextBoxes = GetTextBoxes("txtCenterX", "txtCenterY");
                var colorButton = FindControlByText<Button>("Color de Figura");

                if (centerTextBoxes.Count != 2)
                {
                    ShowError("No se pudieron encontrar los controles de centro.");
                    return;
                }

                var (centerValid, centerCoords, centerError) = ValidateCoordinates(centerTextBoxes);
                if (!centerValid)
                {
                    ShowError(centerError);
                    return;
                }

                var (boundsValid, boundsError) = ValidateCanvasBounds(centerCoords);
                if (!boundsValid)
                {
                    ShowError(boundsError);
                    return;
                }

                var selectedColor = GetButtonColor(colorButton, Color.Black);
                var center = new Point(centerCoords[0], centerCoords[1]);

                if (selectedAlgorithm.Contains("Círculos"))
                {
                    var radiusTextBox = FindControlByName<TextBox>("txtRadius");
                    if (radiusTextBox == null || !int.TryParse(radiusTextBox.Text, out int radius) || radius <= 0)
                    {
                        ShowError("Por favor ingrese un radio válido (mayor que 0).");
                        return;
                    }

                    if (centerCoords[0] - radius < 0 || centerCoords[0] + radius >= canvasBitmap.Width ||
                        centerCoords[1] - radius < 0 || centerCoords[1] + radius >= canvasBitmap.Height)
                    {
                        ShowError("El círculo se sale del canvas. Ajuste el centro o el radio.");
                        return;
                    }

                    var circle = new Circle(center, radius);
                    UpdateExecutionInfo(selectedAlgorithm, "Ejecutando", $"Círculo: Centro({centerCoords[0]},{centerCoords[1]}) Radio({radius})");
                    await circle.DrawBresenham(picCanvas, canvasBitmap, selectedColor, true, cancellationTokenSource.Token);
                    UpdateExecutionInfo(selectedAlgorithm, "Completado", $"Círculo: Centro({centerCoords[0]},{centerCoords[1]}) Radio({radius})");
                }
                else if (selectedAlgorithm.Contains("Elipses"))
                {
                    var semiAxesTextBoxes = GetTextBoxes("txtSemiA", "txtSemiB");
                    if (semiAxesTextBoxes.Count != 2)
                    {
                        ShowError("No se pudieron encontrar los controles de semi-ejes.");
                        return;
                    }

                    var (axesValid, axes, axesError) = ValidateCoordinates(semiAxesTextBoxes);
                    if (!axesValid || axes[0] <= 0 || axes[1] <= 0)
                    {
                        ShowError("Por favor ingrese valores válidos para los semi-ejes (mayores que 0).");
                        return;
                    }

                    if (centerCoords[0] - axes[0] < 0 || centerCoords[0] + axes[0] >= canvasBitmap.Width ||
                        centerCoords[1] - axes[1] < 0 || centerCoords[1] + axes[1] >= canvasBitmap.Height)
                    {
                        ShowError("La elipse se sale del canvas. Ajuste el centro o los semi-ejes.");
                        return;
                    }

                    var ellipse = new Ellipse(center, axes[0], axes[1]);
                    UpdateExecutionInfo(selectedAlgorithm, "Ejecutando", $"Elipse: Centro({centerCoords[0]},{centerCoords[1]}) Semi-ejes({axes[0]},{axes[1]})");
                    await ellipse.DrawBresenham(picCanvas, canvasBitmap, selectedColor, true, cancellationTokenSource.Token);
                    UpdateExecutionInfo(selectedAlgorithm, "Completado", $"Elipse: Centro({centerCoords[0]},{centerCoords[1]}) Semi-ejes({axes[0]},{axes[1]})");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error al ejecutar el algoritmo: {ex.Message}");
                UpdateExecutionInfo(selectedAlgorithm, "Error", ex.Message);
            }
        }

        private async Task ExecuteFillAlgorithmComplete()
        {
            try
            {
                var seedTextBoxes = GetTextBoxes("txtSeedX", "txtSeedY");
                var colorButton = FindControlByText<Button>("Color de Relleno");

                if (seedTextBoxes.Count != 2)
                {
                    ShowError("No se pudieron encontrar los controles de semilla.");
                    return;
                }

                var (seedValid, seedCoords, seedError) = ValidateCoordinates(seedTextBoxes);
                if (!seedValid)
                {
                    ShowError("Por favor haga click en el canvas para seleccionar la semilla.");
                    return;
                }

                var (boundsValid, boundsError) = ValidateCanvasBounds(seedCoords);
                if (!boundsValid)
                {
                    ShowError(boundsError);
                    return;
                }

                var selectedColor = GetButtonColor(colorButton, Color.Red);
                SetExecutionState(true);
                cancellationTokenSource = new CancellationTokenSource();

                var filler = new Filler(new Point(seedCoords[0], seedCoords[1]));
                UpdateExecutionInfo(selectedAlgorithm, "Ejecutando", $"Semilla: ({seedCoords[0]},{seedCoords[1]})");

                if (selectedAlgorithm.Contains("Flood Fill"))
                {
                    await filler.FlowFill(picCanvas, canvasBitmap, selectedColor, true, cancellationTokenSource.Token);
                }
                else if (selectedAlgorithm.Contains("Scanline Fill"))
                {
                    await filler.ScanlineFill(picCanvas, canvasBitmap, selectedColor, true, cancellationTokenSource.Token);
                }

                UpdateExecutionInfo(selectedAlgorithm, "Completado", $"Semilla: ({seedCoords[0]},{seedCoords[1]})");
            }
            catch (Exception ex)
            {
                ShowError($"Error al ejecutar el algoritmo: {ex.Message}");
                UpdateExecutionInfo(selectedAlgorithm, "Error", ex.Message);
            }
            finally
            {
                SetExecutionState(false);
            }
        }

        #endregion

        #region Original Methods (Simplified)

        private void SetParamsForTab(int tabIndex)
        {
            grbParams.Controls.Clear();

            if (tabIndex != 3)
            {
                clipLinePoints.Clear();
                statusPuntos.Text = "Puntos: 0";
            }

            if (tabIndex != 4)
            {
                ClearCurvePoints();
            }

            switch (tabIndex)
            {
                case 0: BuildLineParameterPanel(); break;
                case 1: BuildShapeParameterPanel(); break;
                case 2: BuildFillParameterPanel(); break;
                case 3: BuildClipParameterPanel(); break;
                case 4: BuildCurveParameterPanel(); break;
            }
        }

        private void UpdateSelectedAlgorithmForCurrentTab()
        {
            if (tabAlgorithms.TabPages.Count == 0 || tabAlgorithms.SelectedIndex < 0)
                return;

            var currentTab = tabAlgorithms.TabPages[tabAlgorithms.SelectedIndex];
            foreach (Control control in currentTab.Controls)
            {
                if (control is FlowLayoutPanel panel)
                {
                    foreach (Control radioControl in panel.Controls)
                    {
                        if (radioControl is RadioButton rb && rb.Checked)
                        {
                            selectedAlgorithm = rb.Text;
                            return;
                        }
                    }
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Simulador de Algoritmos de Rasterización";
            this.Size = new Size(1200, 700);
            this.MinimumSize = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.LightGray;

            // Inicializar bitmap para el canvas
            canvasBitmap = new Bitmap(800, 600);
            using (var g = Graphics.FromImage(canvasBitmap))
            {
                g.Clear(Color.White);
            }

            // Barra de estado
            var status = new StatusStrip();
            statusMouse = new ToolStripStatusLabel("Mouse: (0, 0)");
            statusModo = new ToolStripStatusLabel("Modo: Desactivado");
            statusEstado = new ToolStripStatusLabel("Estado: Listo");
            statusPuntos = new ToolStripStatusLabel("Puntos: 0");
            status.Items.AddRange(new[] { statusMouse, statusModo, statusEstado, statusPuntos });
            this.Controls.Add(status);

            // Panel de control
            var controlPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 320,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(controlPanel);

            // Panel contenedor con borde negro para el canvas
            var canvasContainer = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Left = 322,
                Top = 0,
                Width = this.ClientSize.Width - 322,
                Height = this.ClientSize.Height - status.Height,
                BackColor = Color.Black,
                Padding = new Padding(2)
            };

            // Canvas dentro del contenedor
            picCanvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Image = canvasBitmap,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            canvasContainer.Controls.Add(picCanvas);
            this.Controls.Add(canvasContainer);

            // Actualizar el evento Resize para el contenedor
            this.Resize += (s, e) =>
            {
                if (canvasContainer != null && status != null)
                {
                    canvasContainer.Width = this.ClientSize.Width - 322;
                    canvasContainer.Height = this.ClientSize.Height - status.Height;
                }
            };

            // TabControl
            tabAlgorithms = new TabControl
            {
                Dock = DockStyle.Top,
                Height = 180
            };

            // Pestañas
            var lineTab = new TabPage("Líneas");
            lineTab.Controls.Add(CreateRadioGroup(new[] {
                            "DDA (Digital Differential Analyzer)",
                            "Bresenham para Líneas"
                        }));
            tabAlgorithms.TabPages.Add(lineTab);

            var shapeTab = new TabPage("Figuras");
            shapeTab.Controls.Add(CreateRadioGroup(new[] {
                            "Bresenham para Círculos",
                            "Bresenham para Elipses"
                        }));
            tabAlgorithms.TabPages.Add(shapeTab);

            var fillTab = new TabPage("Relleno");
            fillTab.Controls.Add(CreateRadioGroup(new[] {
                            "Flood Fill",
                            "Scanline Fill"
                        }));
            tabAlgorithms.TabPages.Add(fillTab);

            var clipTab = new TabPage("Recorte");
            clipTab.Controls.Add(CreateRadioGroup(new[] {
                            "Cohen-Sutherland",
                            "Sutherland-Hodgman"
                        }));
            tabAlgorithms.TabPages.Add(clipTab);

            var curveTab = new TabPage("Curvas Paramétricas");
            curveTab.Controls.Add(CreateRadioGroup(new[] {
                            "Curvas de Bézier",
                            "B-Splines"
                        }));
            tabAlgorithms.TabPages.Add(curveTab);

            tabAlgorithms.SelectedIndexChanged += (s, e) =>
            {
                if (previousTabIndex == 3 && tabAlgorithms.SelectedIndex != 3)
                {
                    using (var g = Graphics.FromImage(canvasBitmap))
                    {
                        g.Clear(Color.White);
                    }
                    picCanvas.Invalidate();
                }

                SetParamsForTab(tabAlgorithms.SelectedIndex);
                UpdateSelectedAlgorithmForCurrentTab();
                UpdateInfoForCurrentTab();

                previousTabIndex = tabAlgorithms.SelectedIndex;
            };

            controlPanel.Controls.Add(tabAlgorithms);

            // Parámetros
            grbParams = new GroupBox
            {
                Text = "Parámetros",
                Dock = DockStyle.Top,
                Height = 160,
                Padding = new Padding(10)
            };
            controlPanel.Controls.Add(grbParams);

            SetParamsForTab(0);
            UpdateSelectedAlgorithmForCurrentTab();

            // Ejecución
            var grbExec = new GroupBox
            {
                Text = "Ejecución",
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(10)
            };

            btnCompleto = new Button { Text = "Ejecutar", Width = 280, Left = 10, Top = 20 };
            btnPausar = new Button { Text = "Pausar", Width = 130, Left = 10, Top = 60, Enabled = false };
            btnReset = new Button { Text = "Reiniciar", Width = 130, Left = 150, Top = 60 };

            var lblSpeed = new Label { Text = "Velocidad:", Left = 10, Top = 100, Width = 60 };
            sldSpeed = new TrackBar { Minimum = 1, Maximum = 10, Value = 5, TickStyle = TickStyle.None, Left = 80, Top = 95, Width = 200 };

            // Eventos de los botones
            btnCompleto.Click += BtnCompleto_Click;
            btnPausar.Click += BtnPausar_Click;
            btnReset.Click += BtnReset_Click;

            grbExec.Controls.AddRange(new Control[] {
                            btnCompleto, btnPausar, btnReset, lblSpeed, sldSpeed
                        });
            controlPanel.Controls.Add(grbExec);

            // Información
            var grbInfo = new GroupBox
            {
                Text = "Información del Algoritmo",
                Dock = DockStyle.Top,
                Height = 100
            };

            lblInfo = new Label
            {
                Text = "Algoritmo: DDA\nEstado: Listo\nEntradas: Punto inicial y final",
                Top = 20,
                Left = 10,
                AutoSize = true
            };
            grbInfo.Controls.Add(lblInfo);
            controlPanel.Controls.Add(grbInfo);

            // Evento de mouse
            picCanvas.MouseMove += (s, e) =>
            {
                statusMouse.Text = $"Mouse: ({e.X}, {e.Y})";
            };

            picCanvas.MouseClick += PicCanvas_MouseClick;

            // Inicializar información
            UpdateInfoForCurrentTab();
            selectedAlgorithm = "DDA (Digital Differential Analyzer)";
        }

        private async void PicCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (isExecuting) return;

            if (tabAlgorithms.SelectedIndex == 2) // Relleno
            {
                var seedTextBoxes = GetTextBoxes("txtSeedX", "txtSeedY");
                if (seedTextBoxes.ContainsKey("txtSeedX"))
                    seedTextBoxes["txtSeedX"].Text = e.X.ToString();
                if (seedTextBoxes.ContainsKey("txtSeedY"))
                    seedTextBoxes["txtSeedY"].Text = e.Y.ToString();

                await ExecuteFillAlgorithmComplete();
            }
            else if (tabAlgorithms.SelectedIndex == 3) // Recorte
            {
                var areaColorButton = FindControlByText<Button>("Color de Área");
                var areaColor = GetButtonColor(areaColorButton, Color.Red);

                if (selectedAlgorithm.Contains("Cohen-Sutherland"))
                {
                    clipLinePoints.Add(new Point(e.X, e.Y));
                    statusPuntos.Text = $"Puntos: {clipLinePoints.Count}/2";

                    using (var g = Graphics.FromImage(canvasBitmap))
                    {
                        g.Clear(Color.White);
                    }
                    await clipper.DrawClippingArea(picCanvas, canvasBitmap, areaColor, CancellationToken.None);

                    using (var g = Graphics.FromImage(canvasBitmap))
                    {
                        g.FillEllipse(Brushes.Green, e.X - 2, e.Y - 2, 4, 4);
                    }
                    picCanvas.Invalidate();

                    if (clipLinePoints.Count == 2)
                    {
                        var tempLine = new Line(clipLinePoints[0], clipLinePoints[1]);
                        await tempLine.DrawDDA(picCanvas, canvasBitmap, Color.DarkGreen, false, CancellationToken.None);

                        UpdateExecutionInfo(selectedAlgorithm, "Listo",
                            $"Línea definida: ({clipLinePoints[0].X},{clipLinePoints[0].Y}) -> ({clipLinePoints[1].X},{clipLinePoints[1].Y})\nPresione Ejecutar para recortar");
                    }
                    else if (clipLinePoints.Count > 2)
                    {
                        clipLinePoints.Clear();
                        clipLinePoints.Add(new Point(e.X, e.Y));
                        statusPuntos.Text = "Puntos: 1/2";

                        using (var g = Graphics.FromImage(canvasBitmap))
                        {
                            g.Clear(Color.White);
                        }
                        await clipper.DrawClippingArea(picCanvas, canvasBitmap, areaColor, CancellationToken.None);

                        using (var g = Graphics.FromImage(canvasBitmap))
                        {
                            g.FillEllipse(Brushes.Green, e.X - 2, e.Y - 2, 4, 4);
                        }
                        picCanvas.Invalidate();
                    }
                }
                else if (selectedAlgorithm.Contains("Sutherland-Hodgman"))
                {
                    if (!isCreatingPolygon)
                    {
                        isCreatingPolygon = true;
                    }

                    clipPolygon.AddVertice(new Point(e.X, e.Y));
                    statusPuntos.Text = $"Puntos: {clipPolygon.Points.Count}";

                    using (var g = Graphics.FromImage(canvasBitmap))
                    {
                        g.Clear(Color.White);
                    }
                    await clipper.DrawClippingArea(picCanvas, canvasBitmap, areaColor, CancellationToken.None);

                    clipPolygon.DrawPoints(picCanvas, canvasBitmap);

                    if (clipPolygon.Points.Count < 3)
                    {
                        UpdateExecutionInfo(selectedAlgorithm, "Creando",
                            $"Polígono: {clipPolygon.Points.Count} vértices\nNecesita al menos 3 puntos");
                    }
                    else
                    {
                        UpdateExecutionInfo(selectedAlgorithm, "Listo",
                            $"Polígono: {clipPolygon.Points.Count} vértices\nPresione Ejecutar para recortar");
                    }
                }
            }
            else if (tabAlgorithms.SelectedIndex == 4) // AGREGAR ESTA SECCIÓN - Curvas
            {
                if (!isCreatingCurve)
                {
                    isCreatingCurve = true;
                }

                curve.AddControlPoint(new Point(e.X, e.Y));
                statusPuntos.Text = $"Puntos: {curve.ControlPoints.Count}";

                // Dibujar punto de control
                using (var g = Graphics.FromImage(canvasBitmap))
                {
                    g.FillEllipse(Brushes.Purple, e.X - 3, e.Y - 3, 6, 6);

                    // Dibujar número del punto
                    using (var font = new Font("Arial", 8))
                    {
                        g.DrawString(curve.ControlPoints.Count.ToString(), font, Brushes.Black, e.X + 5, e.Y - 5);
                    }
                }
                picCanvas.Invalidate();

                // Actualizar información según el algoritmo y cantidad de puntos
                if (selectedAlgorithm.Contains("Bézier"))
                {
                    if (curve.ControlPoints.Count < 2)
                    {
                        UpdateExecutionInfo(selectedAlgorithm, "Creando",
                            $"Puntos de control: {curve.ControlPoints.Count}\nNecesita al menos 2 puntos");
                    }
                    else
                    {
                        UpdateExecutionInfo(selectedAlgorithm, "Listo",
                            $"Puntos de control: {curve.ControlPoints.Count}\nPresione Ejecutar para dibujar");
                    }
                }
                else if (selectedAlgorithm.Contains("B-Splines"))
                {
                    if (curve.ControlPoints.Count < 4)
                    {
                        UpdateExecutionInfo(selectedAlgorithm, "Creando",
                            $"Puntos de control: {curve.ControlPoints.Count}\nNecesita al menos 4 puntos");
                    }
                    else
                    {
                        UpdateExecutionInfo(selectedAlgorithm, "Listo",
                            $"Puntos de control: {curve.ControlPoints.Count}\nPresione Ejecutar para dibujar");
                    }
                }
            }
        }

        private async void BtnCompleto_Click(object sender, EventArgs e)
        {
            if (isExecuting) return;

            SetExecutionState(true);
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                switch (tabAlgorithms.SelectedIndex)
                {
                    case 0: await ExecuteLineAlgorithmComplete(); break;
                    case 1: await ExecuteShapeAlgorithmComplete(); break;
                    case 2: await ExecuteFillAlgorithmComplete(); break;
                    case 3: await ExecuteClipAlgorithmComplete(); break;
                    case 4: await ExecuteCurveAlgorithmComplete(); break;
                }
            }
            catch (OperationCanceledException)
            {
                statusEstado.Text = "Estado: Cancelado";
            }
            finally
            {
                SetExecutionState(false);
            }
        }

        private void BtnPausar_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                statusEstado.Text = "Estado: Pausado";
            }
        }

        private async void BtnReset_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            using (var g = Graphics.FromImage(canvasBitmap))
            {
                g.Clear(Color.White);
            }
            picCanvas.Invalidate();

            clipLinePoints.Clear();
            statusPuntos.Text = "Puntos: 0";
            showingClippingArea = false;

            ClearPolygonPoints();
            ClearCurvePoints();
            showingClippingArea = false;

            if (tabAlgorithms.SelectedIndex == 3)
            {
                var areaColorButton = FindControlByText<Button>("Color de Área");
                var areaColor = GetButtonColor(areaColorButton, Color.Red);
                await clipper.DrawClippingArea(picCanvas, canvasBitmap, areaColor, CancellationToken.None);
            }

            statusEstado.Text = "Estado: Reiniciado";
            UpdateInfoForCurrentTab();
        }


        private void SetExecutionState(bool executing)
        {
            isExecuting = executing;
            btnCompleto.Enabled = !executing;
            btnPausar.Enabled = executing;
            btnReset.Enabled = !executing;

            tabAlgorithms.Enabled = !executing;

            foreach (TabPage tab in tabAlgorithms.TabPages)
            {
                foreach (Control control in tab.Controls)
                {
                    if (control is FlowLayoutPanel panel)
                    {
                        foreach (Control radioControl in panel.Controls)
                        {
                            if (radioControl is RadioButton rb)
                            {
                                rb.Enabled = !executing;
                            }
                        }
                    }
                }
            }

            statusEstado.Text = executing ? "Estado: Ejecutando" : "Estado: Listo";
        }

        private void UpdateInfoForCurrentTab()
        {
            if (lblInfo == null) return;

            string info = "";

            switch (tabAlgorithms.SelectedIndex)
            {
                case 0:
                    info = $"Algoritmo: {selectedAlgorithm}\nEstado: Listo\nEntradas: Punto inicial y final";
                    statusModo.Text = "Modo: Desactivado";
                    break;
                case 1:
                    info = $"Algoritmo: {selectedAlgorithm}\nEstado: Listo\nEntradas: Centro y radio/semi-ejes";
                    statusModo.Text = "Modo: Desactivado";
                    break;
                case 2:
                    info = $"Algoritmo: {selectedAlgorithm}\nEstado: Listo\nEntradas: Punto semilla";
                    statusModo.Text = "Modo: Activado";
                    break;
                case 3:
                    if (selectedAlgorithm.Contains("Cohen-Sutherland"))
                    {
                        info = $"Algoritmo: {selectedAlgorithm}\nEstado: Listo\nEntradas: 2 puntos para línea";
                    }
                    else if (selectedAlgorithm.Contains("Sutherland-Hodgman"))
                    {
                        info = $"Algoritmo: {selectedAlgorithm}\nEstado: Listo\nEntradas: Puntos del polígono";
                    }
                    else
                    {
                        info = $"Algoritmo: {selectedAlgorithm}\nEstado: Listo\nEntradas: Según algoritmo";
                    }
                    statusModo.Text = "Modo: Activado";
                    ClearPolygonPoints();
                    break;
                case 4:
                    if (selectedAlgorithm.Contains("Bézier"))
                    {
                        info = $"Algoritmo: {selectedAlgorithm}\nEstado: Listo\nEntradas: Puntos de control (mín. 2)";
                    }
                    else if (selectedAlgorithm.Contains("B-Splines"))
                    {
                        info = $"Algoritmo: {selectedAlgorithm}\nEstado: Listo\nEntradas: Puntos de control (mín. 4)";
                    }
                    else
                    {
                        info = $"Algoritmo: {selectedAlgorithm}\nEstado: Listo\nEntradas: Puntos de control";
                    }
                    statusModo.Text = "Modo: Activado";
                    ClearCurvePoints(); // AGREGAR ESTA LÍNEA
                    break;
            }

            lblInfo.Text = info;
        }

        private void ClearCurvePoints()
        {
            curve.ControlPoints.Clear();
            isCreatingCurve = false;
            statusPuntos.Text = "Puntos: 0";
        }

        private async Task ExecuteClipAlgorithmComplete()
        {
            try
            {
                UpdateClippingAreaFromControls();

                var lineColorButton = FindControlByText<Button>("Color de Línea");
                var lineColor = GetButtonColor(lineColorButton, Color.Blue);
                var areaColorButton = FindControlByText<Button>("Color de Área");
                var areaColor = GetButtonColor(areaColorButton, Color.Red);

                if (selectedAlgorithm.Contains("Cohen-Sutherland"))
                {
                    // Algoritmo Cohen-Sutherland para líneas
                    if (clipLinePoints.Count != 2)
                    {
                        ShowError("Cohen-Sutherland: Haga click en 2 puntos para crear la línea a recortar.");
                        return;
                    }

                    var originalLine = new Line(clipLinePoints[0], clipLinePoints[1]);

                    UpdateExecutionInfo(selectedAlgorithm, "Ejecutando",
                        $"Recortando línea: ({clipLinePoints[0].X},{clipLinePoints[0].Y}) -> ({clipLinePoints[1].X},{clipLinePoints[1].Y})");

                    // Dibujar línea original en gris claro
                    await originalLine.DrawDDA(picCanvas, canvasBitmap, Color.LightGray, false, cancellationTokenSource.Token);

                    // Aplicar Cohen-Sutherland
                    var clippedLine = clipper.CohenSutherlandClip(originalLine);

                    if (clippedLine != null)
                    {
                        await clippedLine.DrawBresenham(picCanvas, canvasBitmap, lineColor, true, cancellationTokenSource.Token);
                        UpdateExecutionInfo(selectedAlgorithm, "Completado",
                            $"Línea recortada: ({clippedLine.Start.X},{clippedLine.Start.Y}) -> ({clippedLine.End.X},{clippedLine.End.Y})");
                    }
                    else
                    {
                        UpdateExecutionInfo(selectedAlgorithm, "Completado", "Línea completamente fuera del área de recorte");
                    }
                }
                else if (selectedAlgorithm.Contains("Sutherland-Hodgman"))
                {
                    // Algoritmo Sutherland-Hodgman para polígonos
                    if (clipPolygon.Points.Count < 3)
                    {
                        ShowError("Sutherland-Hodgman: Se necesitan al menos 3 puntos para formar un polígono.");
                        return;
                    }

                    var originalPolygon = new Polygon(clipPolygon.Points);

                    UpdateExecutionInfo(selectedAlgorithm, "Ejecutando",
                        $"Recortando polígono con {clipPolygon.Points.Count} vértices");

                    // Dibujar polígono original en gris claro
                    originalPolygon.Draw(picCanvas, canvasBitmap, Color.LightGray);

                    // Aplicar Sutherland-Hodgman
                    var clippedPolygon = clipper.SutherlandHodgmanClip(originalPolygon);

                    if (clippedPolygon.Points.Count >= 3)
                    {
                        // Dibujar polígono recortado
                        clippedPolygon.Draw(picCanvas, canvasBitmap, lineColor);

                        // Rellenar el polígono recortado con patrón
                        await FillClippedPolygon(clippedPolygon, lineColor);

                        UpdateExecutionInfo(selectedAlgorithm, "Completado",
                            $"Polígono recortado: {clippedPolygon.Points.Count} vértices resultantes");
                    }
                    else
                    {
                        UpdateExecutionInfo(selectedAlgorithm, "Completado", "Polígono completamente fuera del área de recorte");
                    }
                }

                // Redibujar área de recorte para que esté visible
                await clipper.DrawClippingArea(picCanvas, canvasBitmap, areaColor, cancellationTokenSource.Token);

            }
            catch (Exception ex)
            {
                ShowError($"Error al ejecutar el algoritmo: {ex.Message}");
                UpdateExecutionInfo(selectedAlgorithm, "Error", ex.Message);
            }
        }

        private async Task FillClippedPolygon(Polygon polygon, Color color)
        {
            // Crear un patrón de relleno simple para visualizar el polígono recortado
            if (polygon.Points.Count < 3) return;

            // Encontrar bounding box del polígono
            int minX = polygon.Points.Min(p => p.X);
            int maxX = polygon.Points.Max(p => p.X);
            int minY = polygon.Points.Min(p => p.Y);
            int maxY = polygon.Points.Max(p => p.Y);

            // Aplicar patrón de puntos dentro del polígono
            for (int y = minY; y <= maxY; y += 5)
            {
                for (int x = minX; x <= maxX; x += 5)
                {
                    if (IsPointInPolygon(new Point(x, y), polygon.Points))
                    {
                        canvasBitmap.SetPixel(x, y, Color.FromArgb(100, color));
                    }
                }
            }
            picCanvas.Invalidate();
        }

        private bool IsPointInPolygon(Point point, List<Point> polygon)
        {
            int count = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                Point p1 = polygon[i];
                Point p2 = polygon[(i + 1) % polygon.Count];

                if (((p1.Y <= point.Y) && (point.Y < p2.Y)) || ((p2.Y <= point.Y) && (point.Y < p1.Y)))
                {
                    if (point.X < (p2.X - p1.X) * (point.Y - p1.Y) / (p2.Y - p1.Y) + p1.X)
                    {
                        count++;
                    }
                }
            }
            return count % 2 == 1;
        }


        private async Task ExecuteCurveAlgorithmComplete()
        {
            try
            {
                var colorButton = FindControlByText<Button>("Color de Curva");
                var selectedColor = GetButtonColor(colorButton, Color.Purple);

                if (selectedAlgorithm.Contains("Bézier"))
                {
                    if (curve.ControlPoints.Count < 2)
                    {
                        ShowError("Curvas de Bézier: Se necesitan al menos 2 puntos de control.");
                        return;
                    }

                    UpdateExecutionInfo(selectedAlgorithm, "Ejecutando",
                        $"Dibujando curva de Bézier con {curve.ControlPoints.Count} puntos de control");

                    await curve.DrawBezier(picCanvas, canvasBitmap, selectedColor, 100);

                    UpdateExecutionInfo(selectedAlgorithm, "Completado",
                        $"Curva de Bézier dibujada con {curve.ControlPoints.Count} puntos de control");
                }
                else if (selectedAlgorithm.Contains("B-Splines"))
                {
                    if (curve.ControlPoints.Count < 4)
                    {
                        ShowError("B-Splines: Se necesitan al menos 4 puntos de control.");
                        return;
                    }

                    UpdateExecutionInfo(selectedAlgorithm, "Ejecutando",
                        $"Dibujando B-Spline con {curve.ControlPoints.Count} puntos de control");

                    await curve.DrawBSpline(picCanvas, canvasBitmap, selectedColor, 100);

                    UpdateExecutionInfo(selectedAlgorithm, "Completado",
                        $"B-Spline dibujada con {curve.ControlPoints.Count} puntos de control");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error al ejecutar el algoritmo: {ex.Message}");
                UpdateExecutionInfo(selectedAlgorithm, "Error", ex.Message);
            }
        }



        #endregion

        #region Clipping Methods

        private async void ShowDefaultClippingArea()
        {
            if (!showingClippingArea)
            {
                showingClippingArea = true;
                var areaColorButton = FindControlByText<Button>("Color de Área");
                var areaColor = GetButtonColor(areaColorButton, Color.Red);

                await clipper.DrawClippingArea(picCanvas, canvasBitmap, areaColor, CancellationToken.None);
            }
        }

        private async void UpdateClippingAreaFromControls()
        {
            var clipControls = GetTextBoxes("txtClipX1", "txtClipY1", "txtClipX2", "txtClipY2");

            if (clipControls.Count == 4)
            {
                var (success, coords, error) = ValidateCoordinates(clipControls);
                if (success && coords.Length == 4)
                {
                    var newArea = new Rectangle(
                        Math.Min(coords[0], coords[2]),
                        Math.Min(coords[1], coords[3]),
                        Math.Abs(coords[2] - coords[0]),
                        Math.Abs(coords[3] - coords[1])
                    );

                    clipper.SetClippingArea(newArea);
                    showingClippingArea = false;

                    var areaColorButton = FindControlByText<Button>("Color de Área");
                    var areaColor = GetButtonColor(areaColorButton, Color.Red);

                    using (var g = Graphics.FromImage(canvasBitmap))
                    {
                        g.Clear(Color.White);
                    }
                    await clipper.DrawClippingArea(picCanvas, canvasBitmap, areaColor, CancellationToken.None);

                    // Si hay puntos de línea o polígono, redibujar
                    if (clipLinePoints.Count == 1)
                    {
                        using (var g = Graphics.FromImage(canvasBitmap))
                        {
                            var p = clipLinePoints[0];
                            g.FillEllipse(Brushes.Green, p.X - 2, p.Y - 2, 4, 4);
                        }
                    }
                    else if (clipLinePoints.Count == 2)
                    {
                        var tempLine = new Line(clipLinePoints[0], clipLinePoints[1]);
                        await tempLine.DrawDDA(picCanvas, canvasBitmap, Color.DarkGreen, false, CancellationToken.None);
                    }
                    if (clipPolygon.Points.Count > 0)
                    {
                        clipPolygon.DrawPoints(picCanvas, canvasBitmap);
                    }
                }
            }
        }

        private void ClearPolygonPoints()
        {
            clipPolygon.Points.Clear();
            clipLinePoints.Clear();
            isCreatingPolygon = false;
            statusPuntos.Text = "Puntos: 0";
        }

        #endregion
    }
}
