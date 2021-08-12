# ColorBlending
Integrating color blending as a Unity URP Render Pass

TO USE:

Plug the ColorBlendingPipelineAsset.asset into the graphics render pipeline asset slot in the project graphics settings

Make sure the ColorBlendingPipelineRenderer.asset is assigned to the Pipeline Asset

Add a new Colour Blending Render Feature

Assign ColorBlendRenderPass.mat as the material and make sure the feature is enabled

The feature should be injected prior to post processing

Now you can expose the blending options for Bloom, Vignette and Screen Tint as a component in the post processing volume