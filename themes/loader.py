import configparser
import os

# Return string as a list if it is in CSV, otherwise return the string
def csvParse(string):
    if ',' in string:
        return string.split(',')

    return string

# Turn a config file path into a dict that Tkinter kwargs will understand
def parseConfig(path):
    cfg = configparser.ConfigParser()
    cfg.read(path)

    # Convert information to a dictionary
    data = {section: dict(cfg[section]) for section in cfg}

    # Apply default styles to all elements
    globalStyles = data.get('DEFAULT', {})
    applied = {section: dict(globalStyles, **data[section]) for section in data if section != "DEFAULT" }

    # Split csv values
    return {section: {key: csvParse(applied[section][key]) for key in applied[section]} for section in applied}

# This loads in theming information from a given folder
def load(theme):
    eApp = os.path.join(theme, 'elements/appearance.ini')
    ePos = os.path.join(theme, 'elements/position.ini')
    app = os.path.join(theme, 'application.ini')
    
    return parseConfig(eApp), parseConfig(ePos), parseConfig(app)
