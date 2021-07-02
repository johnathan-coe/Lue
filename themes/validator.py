"""
Check themes for internal and external consistency

This ensures that we can switch between themes without unintended side effects
"""
from functools import reduce
from tkinter import font

FONT_FAMILIES = None

def internal(themeData):
    global FONT_FAMILIES
    """
    Ensure every styling attribute used in a stylesheet
        has a corresponding value in all classes
    """

    if not FONT_FAMILIES:
        FONT_FAMILIES = font.families()

    # Warn the user if a theme uses a font family that is unavailable
    for sectionName in themeData:
        if specifiedFont := themeData[sectionName].get('font', None):
            family = specifiedFont[0]

            if family not in FONT_FAMILIES and ' '.join(family.split()[:-1]) not in FONT_FAMILIES:
                print(f"Warning! Font: '{family}' used in style class '{sectionName}' not found!")

    if len(themeData) == 1: return

    # Ensure the value set is identical for each style class
    sectionNames = list(themeData.keys())
    for i, sectionName in enumerate(sectionNames[1:]):
        prevName = sectionNames[i]

        this = set(themeData[sectionName].keys())
        prev = set(themeData[prevName].keys())

        assert this == prev, \
                f"Attributes: {this ^ prev} must be defined for all styles (Discrepancy between '{prevName}', '{sectionName}'). Try DEFAULT?"
    
def validateChange(fromStyle, toStyle):
    internal(dict(fromStyle, **{'new' + k: toStyle[k] for k in toStyle}))

def external(fromTheme, toTheme):
    validateChange(fromTheme.styles, toTheme.styles)
    validateChange(fromTheme.packStyles, toTheme.packStyles)
    validateChange(fromTheme.appStyle, toTheme.appStyle)
