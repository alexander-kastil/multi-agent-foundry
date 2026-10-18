"""
PDF Converter Utility

Provides utilities to convert PDF documents to base64-encoded images
for use with vision-enabled AI models.

Uses PyMuPDF (fitz) as primary method (no external dependencies),
with pdf2image as fallback (requires poppler).
"""

import base64
from io import BytesIO
from pathlib import Path

# Try to import both PDF conversion libraries
try:
    import fitz  # PyMuPDF
    PYMUPDF_AVAILABLE = True
except ImportError:
    PYMUPDF_AVAILABLE = False

try:
    from pdf2image import convert_from_path
    PDF2IMAGE_AVAILABLE = True
except ImportError:
    PDF2IMAGE_AVAILABLE = False


class PdfConverter:
    """Utility class for converting PDF documents to images."""
    
    @staticmethod
    def pdf_to_base64_image(pdf_path: str | Path, dpi: int = 200) -> str:
        """Convert first page of PDF to base64-encoded PNG image.
        
        Tries multiple conversion methods:
        1. PyMuPDF (fitz) - Recommended, no external dependencies
        2. pdf2image - Requires poppler to be installed
        
        Args:
            pdf_path: Path to PDF file (string or Path object)
            dpi: Resolution for rendering (higher = better quality, default 200)
            
        Returns:
            Base64-encoded PNG image string
            
        Raises:
            FileNotFoundError: If PDF file doesn't exist
            RuntimeError: If no PDF conversion library is available
            Exception: If PDF conversion fails
        """
        pdf_path = Path(pdf_path)
        
        if not pdf_path.exists():
            raise FileNotFoundError(f"PDF file not found: {pdf_path}")
        
        # Method 1: Try PyMuPDF (recommended - no external dependencies)
        if PYMUPDF_AVAILABLE:
            try:
                doc = fitz.open(str(pdf_path))
                page = doc[0]  # First page
                
                # Render page to pixmap (image)
                # Calculate zoom for desired DPI (default PDF DPI is 72)
                zoom = dpi / 72
                mat = fitz.Matrix(zoom, zoom)
                pix = page.get_pixmap(matrix=mat)
                
                # Convert to PNG bytes
                image_bytes = pix.pil_tobytes(format="PNG")
                doc.close()
                
                # Encode to base64
                return base64.b64encode(image_bytes).decode("utf-8")
                
            except Exception as e:
                # If PyMuPDF fails, try fallback
                if PDF2IMAGE_AVAILABLE:
                    pass  # Continue to Method 2
                else:
                    raise Exception(f"PyMuPDF failed and no fallback available: {e}")
        
        # Method 2: Try pdf2image (requires poppler)
        if PDF2IMAGE_AVAILABLE:
            try:
                images = convert_from_path(
                    str(pdf_path), 
                    first_page=1, 
                    last_page=1, 
                    dpi=dpi
                )
                
                # Convert to PNG bytes
                buffer = BytesIO()
                images[0].save(buffer, format="PNG")
                image_bytes = buffer.getvalue()
                
                # Encode to base64
                return base64.b64encode(image_bytes).decode("utf-8")
                
            except Exception as e:
                raise Exception(f"pdf2image failed: {e}")
        
        # No conversion library available
        raise RuntimeError(
            "No PDF conversion library available. Install either:\n"
            "  uv pip install pymupdf  (recommended, no external dependencies)\n"
            "  OR\n"
            "  uv pip install pdf2image  (requires poppler binaries)"
        )
    
    @staticmethod
    def pdf_to_data_uri(pdf_path: str | Path, dpi: int = 200, save_image_path: str | Path | None = None) -> str:
        """Convert first page of PDF to a data URI for direct use in vision models.
        
        Args:
            pdf_path: Path to PDF file
            dpi: Resolution for rendering (higher = better quality, default 200)
            save_image_path: Optional path to save the converted PNG image
            
        Returns:
            Data URI string (data:image/png;base64,...)
        """
        base64_image = PdfConverter.pdf_to_base64_image(pdf_path, dpi)
        
        # Optionally save the image to disk
        if save_image_path:
            import base64
            save_path = Path(save_image_path)
            save_path.parent.mkdir(parents=True, exist_ok=True)
            
            # Decode base64 and save as PNG
            image_bytes = base64.b64decode(base64_image)
            with open(save_path, 'wb') as f:
                f.write(image_bytes)
        
        return f"data:image/png;base64,{base64_image}"
