"""Render the user guide as a deterministic, versioned PDF for Onshape publishing."""

from html import escape
from pathlib import Path
from urllib.parse import urljoin

from markdown_it import MarkdownIt
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import KeepTogether, Paragraph, SimpleDocTemplate, Spacer

from versioning import parse_version

REPOSITORY = "https://github.com/lanwen/homeracker-onshape"


def inline_html(tokens, version):
    """Translate CommonMark inline tokens to ReportLab's small XML vocabulary."""
    pieces = []
    for token in tokens:
        if token.type == "text":
            pieces.append(escape(token.content))
        elif token.type == "code_inline":
            pieces.append(f'<font name="Courier">{escape(token.content)}</font>')
        elif token.type in ("strong_open", "strong_close", "em_open", "em_close"):
            tag = "b" if token.type.startswith("strong") else "i"
            pieces.append(f'<{"/" if token.nesting == -1 else ""}{tag}>')
        elif token.type == "link_open":
            href = urljoin(f"{REPOSITORY}/blob/v{version}/docs/", token.attrGet("href"))
            if href == f"{REPOSITORY}/releases/latest":
                href = f"{REPOSITORY}/releases/tag/v{version}"
            pieces.append(f'<a href="{escape(href, quote=True)}" color="#17617a">')
        elif token.type == "link_close":
            pieces.append("</a>")
        elif token.type == "softbreak":
            pieces.append(" ")
        elif token.type == "hardbreak":
            pieces.append("<br/>")
        else:
            raise ValueError(f"Unsupported guide content: {token.type}")
    return "".join(pieces)


def write_guide(source: Path, output: Path, version: str) -> Path:
    parse_version(version)
    text = source.read_text(encoding="utf-8")
    if not text.strip():
        raise ValueError("The PDF guide needs a non-empty source document")
    body = ParagraphStyle("body", fontName="Helvetica", fontSize=10.5,
                          leading=14, spaceAfter=9, textColor=colors.HexColor("#243342"))
    heading = ParagraphStyle("heading", parent=body, fontName="Helvetica-Bold",
                             fontSize=14, leading=18, spaceBefore=13, keepWithNext=True)
    title = ParagraphStyle("title", parent=heading, fontSize=24, leading=29, spaceBefore=0)
    bullet = ParagraphStyle("bullet", parent=body, leftIndent=13, firstLineIndent=-10,
                            spaceAfter=5)
    code = ParagraphStyle("code", parent=body, fontName="Courier", fontSize=8,
                          leading=11, wordWrap="CJK", backColor=colors.HexColor("#eff3f5"),
                          borderPadding=8, spaceBefore=5, spaceAfter=14)
    story = []
    style = body
    in_list = False
    for token in MarkdownIt("commonmark", {"html": False}).parse(text):
        if token.type == "heading_open":
            style = title if token.tag == "h1" else heading
        elif token.type == "paragraph_open":
            style = bullet if in_list else body
        elif token.type == "bullet_list_open":
            in_list = True
        elif token.type == "bullet_list_close":
            in_list = False
        elif token.type == "inline":
            prefix = "- " if style is bullet else ""
            story.append(Paragraph(prefix + inline_html(token.children, version), style))
            if style is title:
                story.append(Paragraph(f"Quick guide - v{version}", body))
                story.append(Spacer(1, 5))
        elif token.type in ("fence", "code_block"):
            content = escape(token.content.strip()).replace("\n", "<br/>")
            # A pasted document URL should also be clickable in the PDF.
            if token.content.strip().startswith("https://") and "\n" not in token.content.strip():
                content = f'<a href="{escape(token.content.strip(), quote=True)}">{content}</a>'
            story.append(Paragraph(content, code))
        elif token.type not in ("heading_close", "paragraph_close", "list_item_open", "list_item_close"):
            raise ValueError(f"Unsupported guide block: {token.type}")

    def footer(canvas, doc):
        canvas.setFont("Helvetica", 8)
        canvas.setFillColor(colors.HexColor("#536574"))
        canvas.drawString(46, 26, f"HomeRacker for Onshape - v{version}")
        canvas.drawRightString(A4[0] - 46, 26, str(doc.page))

    output.parent.mkdir(parents=True, exist_ok=True)
    document = SimpleDocTemplate(str(output), pagesize=A4, rightMargin=46, leftMargin=46,
                                 topMargin=40, bottomMargin=44,
                                 title=f"HomeRacker for Onshape v{version}",
                                 author="Kirill Merkushev", invariant=1)
    # Keep a section together when it fits on one page. Longer sections may split.
    sections = []
    section = []
    for flowable in story:
        if isinstance(flowable, Paragraph) and flowable.style is heading and section:
            sections.append(KeepTogether(section))
            section = []
        section.append(flowable)
    if section:
        sections.append(KeepTogether(section))
    document.build(sections, onFirstPage=footer, onLaterPages=footer)
    return output
