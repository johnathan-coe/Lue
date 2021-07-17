def tkPaddingToCSS(k, v, prefix='padding'):
    sideA, sideB = {'padx': ('left', 'right'),
                    'pady': ('top', 'bottom')}[k]
    
    out = ""
    if type(v) == list:
        a = v[0] 
        b = v[1]
    else:
        a = v
        b = v

    out += f'{prefix}-{sideA}: {a}px;\n'
    out += f'{prefix}-{sideB}: {b}px;\n'

    return out

def tkFontToCSS(v):
    if len(v) == 2:
        return f'font: {v[1]}pt "{v[0]}";\n'
    elif len(v) == 3:
        return f'font: {v[2]} {v[1]}pt "{v[0]}";\n'
    else:
        print('WARNING: Unsupported tkinter font specification!', v)

def styleDictToCSS(d):
    m = {'bg': 'background', 'fg': 'color', 'justify': 'text-align'}

    out = ''
    for k, v in d.items():
        if k == 'font':
            out += tkFontToCSS(v)
        elif k in ('padx', 'pady'):
            out += tkPaddingToCSS(k, v)
        else:
            out += f'{m.get(k, k)}: {v};\n'

    return out

def packStyleDictToCSS(d):
    out = ''
    for k, v in d.items():
        if k in ('padx', 'pady'):
            out += tkPaddingToCSS(k, v, prefix="margin")

    return out

def generateCSS(style, packs):
    out = ''

    for tag in style:
        out += tag + ' {\n'
        out += styleDictToCSS(style[tag])
        out += "}\n\n"

    for tag in packs:
        out += tag + ' {\n'
        out += packStyleDictToCSS(packs[tag])
        out += "}\n\n"

    return out

