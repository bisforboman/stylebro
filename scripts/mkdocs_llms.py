"""MkDocs hook: writes llms.txt (an index of every page with a one-line description, https://llmstxt.org/) and
llms-full.txt (every page's Markdown) into the built site, so agents find the right page without crawling.

The build fails when a rule page (docs/rules/BROxxxx.md) is missing from llms.txt.
"""

import re

from mkdocs.exceptions import PluginError

_order = []
_pages = {}


def on_nav(nav, config, files):
    _order.clear()
    _order.extend(page.file.src_uri for page in nav.pages)
    _pages.clear()


def on_page_content(html, page, config, files):
    _pages[page.file.src_uri] = (page.title, page.canonical_url, page.markdown)
    return html


def describe(markdown):
    """The first sentence of the page's first paragraph, without Markdown links and emphasis."""
    for block in re.split(r"\n\s*\n", markdown):
        block = block.strip()
        if not block or block.startswith(("#", ">", "|", "- ", "* ", "```", "!!!", "<")):
            continue
        text = " ".join(block.split())
        text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text).replace("**", "")
        return re.split(r"(?<=\.)\s", text, maxsplit=1)[0]
    return ""


def on_post_build(config):
    site = config["site_dir"]
    docs, rules = [], []
    for uri in _order:
        if uri not in _pages:
            continue
        title, url, markdown = _pages[uri]
        line = f"- [{title}]({url}): {describe(markdown)}"
        (rules if re.match(r"rules/BRO\d+\.md$", uri) else docs).append(line)

    missing = [u for u in _pages if re.match(r"rules/BRO\d+\.md$", u) and not any(f"/{u[:-3]}/)" in r for r in rules)]
    if missing:
        raise PluginError(f"llms.txt misses rule pages: {', '.join(missing)}")

    text = (
        f"# {config['site_name']}\n\n> {config['site_description']}\n\n"
        "Install the StyleBro.Analyzers package, run `stylebro-migrate init --write` (or `stylebro-migrate --write` when "
        "coming from StyleCop), then `stylebro-migrate format`. For scripts and AI agents (`--json`, `format --files`, "
        f"AGENTS.md): {config['site_url']}agents/\n\n"
        "## Docs\n\n" + "\n".join(docs) + "\n\n## Rules\n\n" + "\n".join(rules) + "\n"
    )
    with open(f"{site}/llms.txt", "w", encoding="utf-8", newline="\n") as f:
        f.write(text)

    with open(f"{site}/llms-full.txt", "w", encoding="utf-8", newline="\n") as f:
        f.write(f"# {config['site_name']}\n\n> {config['site_description']}\n")
        for uri in _order:
            if uri in _pages:
                title, url, markdown = _pages[uri]
                f.write(f"\n\n---\nSource: {url}\n\n{markdown.strip()}\n")
