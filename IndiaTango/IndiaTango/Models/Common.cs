using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WindowsFormsAero.Dwm;
using WindowsFormsAero.TaskDialog;
using Path = System.IO.Path;

namespace IndiaTango.Models
{
    public static class Common
    {
		public static string TagLine { get { return "[Buoys Buoys Boys]"; } }
    	public static string ApplicationTitle { get { return "Codename B3"; } }
        public static string Version { get { return string.Format("alpha version {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString()); } }
        public static string Creators { get { return "Developed by:\r\nSteven McTainsh\r\nLuke Barnett\r\nMichael Baumberger\r\nKerry Arts"; } }
        
        public static string Icon { get { return "/IndiaTango;component/Images/icon.ico"; } }
		public static string TestDataPath { get { return "../../Test Data/"; } }
        public static string AppDataPath
        {
            get
            {
                var path = Path.Combine(new string[]{
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "IndiaTango",
                });

                if (!Directory.Exists(path)) // Creates directory if it doesn't exist, no need to create beforehand
                    Directory.CreateDirectory(path);

                return path;
            }
        }

    	public static bool CanUseGlass = WindowsFormsAero.OsSupport.IsVistaOrBetter &&
    	                                 WindowsFormsAero.OsSupport.IsCompositionEnabled;
    	                                 
        public static string AddIcon { get { return "/IndiaTango;component/Images/plus.png"; } }
        public static string EditIcon { get { return "/IndiaTango;component/Images/pencil.png"; } }
        public static string DeleteIcon { get { return "/IndiaTango;component/Images/cross-script.png"; } }

    	public static void SetFancyBackground(Window window,Grid mainGrid, bool useGlass, bool useGradient)
    	{
			//Set the glass
			if (useGlass && CanUseGlass)
			{
				//Get handle
				IntPtr hwnd = new WindowInteropHelper(window).Handle;

				//Set backgroung to black and transparent (At the same time!!!)
				window.Background = Brushes.Transparent;
				HwndSource source = HwndSource.FromHwnd(hwnd);
				source.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

				//Set glass
				DwmManager.EnableBlurBehind(hwnd);
			}

			//Create rectangle and set its position and span
			Rectangle rectangle = new Rectangle();
			Grid.SetColumn(rectangle, 0);
			Grid.SetRow(rectangle, 0);
			Grid.SetColumnSpan(rectangle, mainGrid.ColumnDefinitions.Count > 0 ? mainGrid.ColumnDefinitions.Count : 1);
			Grid.SetRowSpan(rectangle, mainGrid.RowDefinitions.Count > 0 ? mainGrid.RowDefinitions.Count : 1);

			if (useGradient)
			{
				//Create the gradient brush
				GradientStopCollection gradients = new GradientStopCollection();
				if (useGlass && CanUseGlass)
				{
					gradients.Add(new GradientStop(Colors.White, 0));
					gradients.Add(new GradientStop(Color.FromArgb(150, 255, 255, 255), 1));
				}
				else
				{
					gradients.Add(new GradientStop(Colors.White, 0));
					gradients.Add(new GradientStop(Colors.White, 0.5));
					gradients.Add(new GradientStop(Color.FromRgb(220, 220, 220), 1));
				}

				rectangle.Fill = new LinearGradientBrush(gradients, new Point(1, 0), new Point(1, 1)); ;
			}
			else
			{
				rectangle.Fill = new SolidColorBrush(Color.FromArgb(200,255,255,255));
			}
    		
			//Insert the rectangle
			mainGrid.Children.Insert(0, rectangle);
    	}

		public static bool ShowMessageBox(string title, string text, bool showCancel, bool isError)
		{
            if (CanUseGlass)
            {
                TaskDialog dialog = new TaskDialog(title, title, text,
                                                   (showCancel ? (TaskDialogButton.OK | TaskDialogButton.Cancel) : TaskDialogButton.OK),
                                                   isError ? TaskDialogIcon.Stop : TaskDialogIcon.Information);
                var result = dialog.Show();
                return result.CommonButton == WindowsFormsAero.TaskDialog.Result.OK;
            }
            else
			{
				return System.Windows.Forms.MessageBox.Show(text, title,
				                                            showCancel ? MessageBoxButtons.OKCancel : MessageBoxButtons.OK,
				                                            isError ? MessageBoxIcon.Error : MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.OK;
			}
		}

		public static void ShowFeatureNotImplementedMessageBox()
		{
			ShowMessageBox("Feature Not Implemented", "Sorry, this feature has not been created yet.", false, false);
		}

        public static Random Generator = new Random();

        public static bool Confirm(string title, string message)
        {
            if(CanUseGlass)
            {
                TaskDialog dialog = new TaskDialog(title, title, message,
                                                   TaskDialogButton.Yes | TaskDialogButton.No,
                                                   TaskDialogIcon.Warning);
                var result = dialog.Show();
                return result.CommonButton == WindowsFormsAero.TaskDialog.Result.Yes;
            }
            else
            {
                return System.Windows.Forms.MessageBox.Show(message, title, MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Warning) == DialogResult.Yes;
            }
        }

        public static void RenderImage(UIElement elementToRender, string filename)
        {
            if (elementToRender == null)
                return;

            var height = (int)elementToRender.RenderSize.Height;
            var width = (int)elementToRender.RenderSize.Width;

            var renderer = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);

            renderer.Render(elementToRender);

            var pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(renderer));

            using (var file = File.Create(filename))
            {
                pngEncoder.Save(file);
            }
        }
    }
}
