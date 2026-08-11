namespace ERP.Mobile;

/// <summary>WebView-də uzun-basmanı udur — mətn seçmə/kopyala-yapışdır menyusu açılmasın.</summary>
public sealed class NoLongClick : Java.Lang.Object, Android.Views.View.IOnLongClickListener
{
    public bool OnLongClick(Android.Views.View? v) => true; // true = hadisə udulur, kontekst menyu açılmır
}
