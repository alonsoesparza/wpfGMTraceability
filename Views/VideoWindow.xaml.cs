using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using wpfGMTraceability.Helpers;

namespace wpfGMTraceability.Views
{
    public partial class VideoWindow : Window
    {
        private class Slide
        {
            public string Url { get; set; } = "";
            public bool IsVideo { get; set; }
            public int DurationSec { get; set; } = 15; // para imágenes
        }

        private readonly HttpClient _http = new HttpClient();
        private readonly List<Slide> _slides = new List<Slide>();
        private readonly DispatcherTimer _imgTimer = new DispatcherTimer();
        private readonly DispatcherTimer _reloadTimer = new DispatcherTimer();

        private int _index = 0;
        private bool _playing = false;
        private Uri _pageUri;
        private string _checkUrl = ""; // /slid/check_for_new_images
        private bool _loadedOnce = false;

        public VideoWindow()
        {
            InitializeComponent();

            _imgTimer.Tick += (s, e) => NextSlideWithFade();
            _reloadTimer.Interval = TimeSpan.FromSeconds(30);
            _reloadTimer.Tick += async (s, e) => await CheckForChangesAndReload();
        }

        private async void VideoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loadedOnce) return;
            _loadedOnce = true;

            try
            {
                // 1) Lee URL del HTML (tu /slid/site) desde Settings.json
                var json = File.ReadAllText(SettingsManager.ConfigSettingsFilePath);
                var cfg = JsonConvert.DeserializeObject<SettingsConfig>(json);
                var pageUrl = (cfg?.VideoURL ?? "").Trim();
                if (string.IsNullOrWhiteSpace(pageUrl))
                {
                    MessageBox.Show("VideoURL vacío en Settings.json");
                    Close();
                    return;
                }

                _pageUri = new Uri(pageUrl);
                // Deriva el endpoint check_for_new_images del mismo host
                // Si tu ruta difiere, cámbiala aquí.
                _checkUrl = new Uri(_pageUri, "/slid/check_for_new_images").ToString();

                // 2) Carga y parsea el HTML para obtener la lista de slides
                await LoadSlidesFromHtml(pageUrl);

                if (_slides.Count == 0)
                {
                    MessageBox.Show("No se encontraron slides en la página.");
                    Close();
                    return;
                }

                // 3) Inicia reproducción
                _index = 0;
                await ShowCurrentSlide();
                _reloadTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar slides: " + ex.Message);
                Close();
            }
        }

        // Descarga el HTML, extrae <img> y <video><source type='video/mp4'>
        private async Task LoadSlidesFromHtml(string pageUrl)
        {
            _slides.Clear();

            var html = await _http.GetStringAsync(pageUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Elementos con class="slide"
            var nodes = doc.DocumentNode.SelectNodes("//*[@class and contains(concat(' ', normalize-space(@class), ' '), ' slide ')]");
            if (nodes == null) return;

            foreach (var node in nodes)
            {
                if (node.Name.Equals("img", StringComparison.OrdinalIgnoreCase))
                {
                    var src = node.GetAttributeValue("src", "");
                    // data-duracion (segundos)
                    var durAttr = node.GetAttributeValue("data-duracion", "15");
                    int dur = 15;
                    int.TryParse(durAttr, out dur);

                    var abs = ToAbsoluteUrl(src);
                    if (!string.IsNullOrWhiteSpace(abs))
                        _slides.Add(new Slide { Url = abs, IsVideo = false, DurationSec = Math.Max(1, dur) });
                }
                else if (node.Name.Equals("video", StringComparison.OrdinalIgnoreCase))
                {
                    // buscar <source type="video/mp4">
                    var source = node.SelectSingleNode(".//source[@src]");
                    var src = source?.GetAttributeValue("src", "") ?? "";
                    var type = source?.GetAttributeValue("type", "") ?? "";
                    if (!string.IsNullOrWhiteSpace(src) && type.Contains("mp4"))
                    {
                        var abs = ToAbsoluteUrl(src);
                        if (!string.IsNullOrWhiteSpace(abs))
                            _slides.Add(new Slide { Url = abs, IsVideo = true });
                    }
                }
            }
        }

        private string ToAbsoluteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || _pageUri == null) return "";
            if (Uri.TryCreate(url, UriKind.Absolute, out var abs))
                return abs.ToString();
            // relativo al host de la página
            var combined = new Uri(_pageUri, url);
            return combined.ToString();
        }

        private async Task ShowCurrentSlide()
        {
            if (_playing || _slides.Count == 0) return;
            _playing = true;

            var slide = _slides[_index];

            // Fade-out
            await FadeTo(1.0, 0.4);

            // Oculta ambos
            ImgSlide.Visibility = Visibility.Collapsed;
            VidSlide.Visibility = Visibility.Collapsed;
            VidSlide.Stop();

            if (slide.IsVideo)
            {
                // Video
                VidSlide.Source = new Uri(slide.Url);
                VidSlide.Visibility = Visibility.Visible;
                VidSlide.Position = TimeSpan.Zero;
                VidSlide.Volume = 0;      // silencioso (autoplay-friendly)
                VidSlide.IsMuted = true;  // asegúrate de muted
                VidSlide.Play();
                // cuando termine, VidSlide_MediaEnded llamará NextSlideWithFade()
            }
            else
            {
                // Imagen
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(slide.Url);
                bi.EndInit();
                ImgSlide.Source = bi;
                ImgSlide.Visibility = Visibility.Visible;

                // programa cambio según duración
                _imgTimer.Stop();
                _imgTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, slide.DurationSec));
                _imgTimer.Start();
            }

            // Fade-in
            await FadeTo(0.0, 0.4);

            _playing = false;
        }

        private async void NextSlideWithFade()
        {
            _imgTimer.Stop();
            _index = (_index + 1) % _slides.Count;
            await ShowCurrentSlide();
        }

        private async void VidSlide_MediaEnded(object sender, RoutedEventArgs e)
        {
            NextSlideWithFade();
        }

        private async Task FadeTo(double target, double seconds)
        {
            var tcs = new TaskCompletionSource<bool>();
            var anim = new DoubleAnimation
            {
                To = target,
                Duration = TimeSpan.FromSeconds(seconds),
                FillBehavior = FillBehavior.HoldEnd
            };
            anim.Completed += (s, e) => tcs.TrySetResult(true);
            FadeOverlay.BeginAnimation(UIElement.OpacityProperty, anim);
            await tcs.Task;
        }

        private async Task CheckForChangesAndReload()
        {
            try
            {
                if (string.IsNullOrEmpty(_checkUrl))
                    return;

                var resp = await _http.GetAsync(_checkUrl);
                var txt = await resp.Content.ReadAsStringAsync();

                // esperamos algo como: {"images_changed": true/false}
                if (txt.Contains("\"images_changed\": true"))
                {
                    if (_pageUri != null)
                    {
                        await LoadSlidesFromHtml(_pageUri.ToString());
                        _index = 0;
                        await ShowCurrentSlide();
                    }
                }
            }
            catch
            {
                // ignora errores de red; reintenta en el próximo tick
            }
        }


        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try { VidSlide.Stop(); } catch { }
            _imgTimer.Stop();
            _reloadTimer.Stop();
            Close();
        }
    }
}
