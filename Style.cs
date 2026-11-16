using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
namespace Ilha {
public partial class App {
 // Implicit styles so every screen gets thin scrollbars and dark rounded dropdowns instead of Windows defaults.
 static void ApplyStyles(){
  var styles=(ResourceDictionary)XamlReader.Parse(@"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='ScrollBar'><Setter Property='Width' Value='6'/><Setter Property='MinWidth' Value='0'/><Setter Property='HorizontalAlignment' Value='Right'/><Setter Property='Background' Value='Transparent'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollBar'><Grid Background='Transparent'><Track x:Name='PART_Track' IsDirectionReversed='True'><Track.Thumb><Thumb><Thumb.Template><ControlTemplate TargetType='Thumb'><Border CornerRadius='3' Background='#50FFFFFF' Margin='1,0'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb></Track></Grid></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='ComboBoxItem'><Setter Property='Foreground' Value='#FFF8F6'/><Setter Property='Padding' Value='10,5'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBoxItem'><Border x:Name='b' CornerRadius='7' Padding='{TemplateBinding Padding}' Background='Transparent'><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Setter TargetName='b' Property='Background' Value='#33FFFFFF'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='ComboBox'><Setter Property='Foreground' Value='#FFF8F6'/><Setter Property='FontSize' Value='12'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBox'><Grid>
<ToggleButton Focusable='False' ClickMode='Press' IsChecked='{Binding IsDropDownOpen,Mode=TwoWay,RelativeSource={RelativeSource TemplatedParent}}'><ToggleButton.Template><ControlTemplate TargetType='ToggleButton'><Border Background='#26FFFFFF' CornerRadius='10'><Path Data='M0,0 L4,4 8,0' Stroke='#B0FFFFFF' StrokeThickness='1.5' StrokeStartLineCap='Round' StrokeEndLineCap='Round' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,12,0'/></Border></ControlTemplate></ToggleButton.Template></ToggleButton>
<ContentPresenter Content='{TemplateBinding SelectionBoxItem}' Margin='12,5,28,5' IsHitTestVisible='False' VerticalAlignment='Center' HorizontalAlignment='Left'/>
<Popup IsOpen='{TemplateBinding IsDropDownOpen}' Placement='Bottom' AllowsTransparency='True' Focusable='False' PopupAnimation='Fade'><Border MinWidth='{TemplateBinding ActualWidth}' MaxHeight='170' Background='#F4121216' CornerRadius='10' Margin='0,4,12,12' Padding='4'><ScrollViewer><StackPanel IsItemsHost='True'/></ScrollViewer></Border></Popup>
</Grid></ControlTemplate></Setter.Value></Setter></Style>
</ResourceDictionary>");
  w.Resources.MergedDictionaries.Add(styles);
 }
}
}
