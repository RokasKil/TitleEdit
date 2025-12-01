using Dalamud.Bindings.ImGui;
using System;
using Dalamud.Interface.Utility.Raii;

namespace TitleEdit.Windows.Tabs
{
    internal abstract class AbstractTab : ITab
    {
        protected bool modal;
        protected string modalTitle = "";
        protected Action? modalContent = null;

        protected Exception? lastException = null;

        public abstract string Title { get; }

        public virtual void Draw()
        {
            using var id = ImRaii.PushId(Title);
            DrawModal();
        }

        protected void DrawModal()
        {
            if (modal)
            {
                using var id = ImRaii.PushId(Title);
                ImGui.OpenPopup(Title);
                using var popupModal = ImRaii.PopupModal(Title, ref modal, ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize);
                if (popupModal)
                {
                    modalContent?.Invoke();
                }
            }
        }

        protected void SetupModal(string modalTitle, Action? modalContent, Exception? ex = null)
        {
            if (ex is AggregateException { InnerException: not null } aex)
            {
                lastException = aex.InnerException;
            }
            else
            {
                lastException = ex;
            }

            modal = true;
            this.modalTitle = modalTitle;
            this.modalContent = modalContent;
        }

        protected void SetupError(Exception ex) => SetupModal("Error", DrawError, ex);

        protected void DrawError()
        {
            ImGui.TextUnformatted("Error encountered: ");
            ImGui.TextUnformatted($"{lastException?.Message}");
            if (ImGui.Button("Ok")) CloseModal();
        }

        protected void CloseModal() => modal = false;
    }
}
