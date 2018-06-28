from Naked.toolshed.shell import execute_js, muterun_js

success = execute_js('testscript.js')

# import sys
# from Naked.toolshed.shell import muterun_js

# response = muterun_js('testscript.js')
# if response.exitcode == 0:
#   print(response.stdout)
# else:
#   sys.stderr.write(response.stderr)
