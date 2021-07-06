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
    m = {'bg': 'background', 'fg': 'color'}

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

def generateCSS(elemMap, elems, theme):
    out = ''
    
    out += 'body {\n'
    out += styleDictToCSS(theme.appStyle['Frame'])
    out += '}\n'

    for elem in elems:
        out += elemMap.get(elem, elem) + ' {\n'
        out += styleDictToCSS(theme.styles.get(elem, theme.styles.get('body', {})))
        out += packStyleDictToCSS(theme.packStyles.get(elem, theme.packStyles.get('body', {})))
        out += "}\n\n"

    return out

