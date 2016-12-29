using System;
using System.IO;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;
using Sitecore.Mvc.Presentation;

namespace Boondoggle.Sc.WarnableExecuteRenderer.Pipelines.Mvc.RenderRendering
{
    public class WarnableExecuteRenderer : ExecuteRenderer
    {
        private readonly string _renderingModelErrorMessage;
        private readonly string _errorHtmlTemplate;
        private readonly bool _renderErrorOnlyWhileEditing;

        public WarnableExecuteRenderer() : base()
        {
            _renderingModelErrorMessage = Sitecore.Configuration.Settings.GetSetting("WarnableExecuteRenderer.ErrorMessage",
                "The model item passed into the dictionary is of type 'Sitecore.Mvc.Presentation.RenderingModel'");
            _errorHtmlTemplate = Sitecore.Configuration.Settings.GetSetting("WarnableExecuteRenderer.HtmlTemplate",
                "<div style=\"padding: 10px; background-color: red;\"><h1>An error occured while trying to render this component.</h1><ul><li>Placeholder: '{0}'</li><li>Rendering: '{1}'</li><li>Datasource: '{2}'</li></ul></div>");
            _renderErrorOnlyWhileEditing =
                Sitecore.Configuration.Settings.GetBoolSetting("WarnableExecuteRenderer.ErrorOnlyWhileEditing", false);
        }

        #region Overrides of ExecuteRenderer

        protected override bool Render(Renderer renderer, TextWriter writer, RenderRenderingArgs args)
        {
            try
            {
                return base.Render(renderer, writer, args);
            }
            catch (InvalidOperationException invalidOperationException)
            {
                if (!IsRenderingModelError(invalidOperationException)) throw;
                if (args.Rendering == null || args.Rendering.Renderer == null) throw;

                var renderingString = "";
                if (args.Rendering.Renderer is ViewRenderer)
                {
                    var rendering = ((ViewRenderer)args.Rendering.Renderer);
                    renderingString = rendering.ViewPath;
                }
                else if (args.Rendering.Renderer is ControllerRenderer)
                {
                    var rendering = ((ControllerRenderer)args.Rendering.Renderer);
                    renderingString = rendering.ControllerName + "." + rendering.ActionName;
                }

                var errorMessage =
                    string.Format(
                        "Unable to render rendering due to missing datasource. Placeholder: '{0}', Rendering: '{1}', Datasource: '{2}'",
                        args.Rendering.Placeholder, renderingString, args.Rendering.DataSource);

                Log.Error(errorMessage, invalidOperationException, GetType());

                if ((_renderErrorOnlyWhileEditing && Sitecore.Context.PageMode.IsExperienceEditorEditing) ||
                    !_renderErrorOnlyWhileEditing)
                {
                    writer.WriteLine(_errorHtmlTemplate, args.Rendering.Placeholder, renderingString,
                        args.Rendering.DataSource);
                }
                else
                {
                    throw new Exception(errorMessage, invalidOperationException);
                }

                return true;

            }
        }

        #endregion

        private bool IsRenderingModelError(InvalidOperationException exception)
        {
            if (exception == null) return false;
            if (exception.InnerException == null) return false;
            if (string.IsNullOrWhiteSpace(exception.InnerException.Message)) return false;

            return exception.InnerException.Message.StartsWith(_renderingModelErrorMessage);
        }
    }
}
