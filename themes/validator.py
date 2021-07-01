"""
Check themes for internal and external consistency

This ensures that we can switch between themes without unintended side effects
"""
from functools import reduce

def internal(themeData):
    """
    Ensure every styling attribute used in a stylesheet
        has a corresponding value in all classes
    """

    if len(themeData) == 1: return

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
