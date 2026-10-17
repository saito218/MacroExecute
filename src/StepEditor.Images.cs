using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace MacroExecute
{
    internal sealed partial class StepEditor
    {
        private void MultiImageFields()
        {
            Section("Image alternatives  -  checked from top to bottom");
            var list=new ListBox{Dock=DockStyle.Fill,AccessibleName="Ordered reference images",IntegralHeight=false};
            Action refresh=delegate{list.Items.Clear();list.Items.Add("1  -  Primary reference (preview above)");for(int i=0;i<Value.AdditionalImages.Length;i++)list.Items.Add((i+2)+"  -  Embedded alternative");list.SelectedIndex=0;};refresh();list.SelectedIndexChanged+=delegate{int index=list.SelectedIndex;if(index<0)return;if(index==0){UpdatePreview();return;}try{using(var image=ImageTemplate.Load(new Step{ImageData=Value.AdditionalImages[index-1]})){if(preview.Image!=null)preview.Image.Dispose();preview.Image=new Bitmap(image);imageInfo.Text="Reference "+(index+1)+"  -  "+image.Width+" x "+image.Height+" px";}}catch(Exception error){validationMessage.Text=error.GetBaseException().Message;}};Row("Search order",list,110);list.AccessibleName="Ordered reference images";
            var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true};
            Action<List<string>> store=delegate(List<string> images){if(images.Count>64)throw new InvalidDataException("Use at most 64 images.");Value.ImageData=images[0];Value.Target="";images.RemoveAt(0);Value.AdditionalImages=images.ToArray();refresh();UpdatePreview();};
            Func<List<string>> references=delegate{if(string.IsNullOrEmpty(Value.ImageData)) {var copy=Value.Clone();if(!Path.IsPathRooted(copy.Target))copy.Target=Path.Combine(directory,copy.Target);ImageTemplate.Embed(copy);Value.ImageData=copy.ImageData;}var images=new List<string>{Value.ImageData};images.AddRange(Value.AdditionalImages);return images;};
            Button(actions,"Add images...",delegate{using(var dialog=new OpenFileDialog{Multiselect=true,Filter="Images|*.png;*.bmp;*.jpg;*.jpeg;*.gif"})if(dialog.ShowDialog(this)==DialogResult.OK)try{ApplyDraft();var images=new List<string>();if(!string.IsNullOrWhiteSpace(Value.ImageData)||!string.IsNullOrWhiteSpace(Value.Target))images=references();foreach(string file in dialog.FileNames)using(var image=ImageTemplate.Load(new Step{Target=file}))images.Add(ImageTemplate.Encode(image));store(images);}catch(Exception error){validationMessage.Text=error.GetBaseException().Message;}});
            Button(actions,"Capture +",async delegate{try{if(captureActive||testCancellation!=null)return;if(Value.AdditionalImages.Length>=63){validationMessage.Text="Use at most 64 images.";return;}ApplyDraft();string original=Value.ImageData,target=Value.Target;Value.ImageData="";Value.Target="";await CaptureReference();if(IsDisposed)return;string added=Value.ImageData;Value.ImageData=original;Value.Target=target;if(!string.IsNullOrEmpty(added)){if(string.IsNullOrEmpty(original)&&string.IsNullOrEmpty(target))Value.ImageData=added;else {var images=references();images.Add(added);store(images);}}Rebuild();}catch(Exception error){if(!IsDisposed)validationMessage.Text=error.GetBaseException().Message;}});
            Button(actions,"Remove",delegate{try{ApplyDraft();var images=references();int selected=list.SelectedIndex;if(images.Count<=1||selected<0)return;images.RemoveAt(selected);store(images);}catch(Exception error){validationMessage.Text=error.GetBaseException().Message;}});
            foreach(int direction in new[]{-1,1}){int move=direction;Button(actions,move<0?"Up":"Down",delegate{try{ApplyDraft();var images=references();int selected=list.SelectedIndex,next=selected+move;if(selected<0||next<0||next>=images.Count)return;string item=images[selected];images[selected]=images[next];images[next]=item;store(images);list.SelectedIndex=next;}catch(Exception error){validationMessage.Text=error.GetBaseException().Message;}});}
            Row("Manage images",actions,80);
            Check("Require ALL reference images",Value.RequireAllImages,delegate(bool value){Value.RequireAllImages=value;});
            Row("",new Label{Text="Off: try each image in order and stop at the first hit. On: every image must be present in the same captured screen; stop at the first missing image. Shared search area, tolerance and timeout apply. The first matching image is the pointer/click target; Click image clicks once. Inverted IF negates the complete result.",Tag="muted"},110);
        }
    }
}
