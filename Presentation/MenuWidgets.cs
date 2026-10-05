using Rivet;
namespace Rivet.Homeland.Presentation;
internal static class MenuWidgets
{
    public static bool Button(Engine e,UiRect r,string text,bool enabled=true,bool primary=false){var over=enabled&&r.Contains(e.Input.MousePosition);e.Render2D.RoundedRectangle(r.X,r.Y,r.W,r.H,5,over?HomelandTheme.Text:primary?HomelandTheme.Accent:HomelandTheme.Panel);e.Render2D.TextBox(text,r.X+8,r.Y,r.W-16,r.H,16,over||primary?HomelandTheme.Background:enabled?HomelandTheme.Text:HomelandTheme.Muted,TextHorizontalAlignment.Center,TextVerticalAlignment.Center,false);return over&&e.Input.MousePressed(MouseButton.Left);}
    public static void Backdrop(Engine e,string subtitle){var v=e.Input.ViewportSize;e.Render2D.Rectangle(0,0,v.X,v.Y,HomelandTheme.Background);e.Render2D.Rectangle(0,0,v.X,3,HomelandTheme.Accent);e.Render2D.Text("HOMELAND",48,38,38,HomelandTheme.Text);e.Render2D.Text(subtitle,50,88,14,HomelandTheme.Accent);}
}
