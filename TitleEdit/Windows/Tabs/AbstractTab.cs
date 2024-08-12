using ImGuiNET;
using System;

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
            DrawModal();
        }

        protected void DrawModal()
        {
            if (modal)
            {
                ImGui.OpenPopup($"{modalTitle}##{Title}");
                if (ImGui.BeginPopupModal($"{modalTitle}##{Title}", ref modal, ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize))
                {
                    modalContent?.Invoke();
                    ImGui.EndPopup();
                }
            }
        }

        protected void SetupModal(string modalTitle, Action? modalContent, Exception? ex = null)
        {
            lastException = ex;
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
