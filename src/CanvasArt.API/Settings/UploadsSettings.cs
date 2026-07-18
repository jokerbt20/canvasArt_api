namespace CanvasArt.API.Settings;

/// <summary>
/// Single source of truth for where uploaded media physically lives and how its public URL is
/// built. Identical code path in every environment — only these values change between
/// local dev and production.
/// </summary>
public sealed class UploadsSettings
{
    public const string SectionName = "Uploads";

    /// <summary>Public base URL prefix (no trailing slash) that serves the physical root, e.g. "https://canvasarts.mk/uploads".</summary>
    public string BaseUrl { get; set; } = "https://canvasarts.mk/uploads";

    /// <summary>
    /// Physical folder that directly contains the images/thumbs/frames sub-folders served
    /// statically. Absolute, or relative to the application content root. In production this
    /// should point outside the deployed app folder so redeploys never touch uploaded files.
    /// </summary>
    public string PhysicalRoot { get; set; } = "uploads";

    /// <summary>Sub-folder holding resized (non-watermarked) and watermarked painting/slide variants.</summary>
    public string ImagesFolder { get; set; } = "images";

    /// <summary>Sub-folder holding thumbnail variants.</summary>
    public string ThumbsFolder { get; set; } = "thumbs";

    /// <summary>Sub-folder holding frame images and thumbnails.</summary>
    public string FramesFolder { get; set; } = "frames";

    /// <summary>Sub-folder holding customer testimonial photos and thumbnails.</summary>
    public string CustomersFolder { get; set; } = "customers";
}
