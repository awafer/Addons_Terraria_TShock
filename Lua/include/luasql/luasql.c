/*
** $Id: luasql.c,v 1.28 2009/02/11 12:08:50 tomas Exp $
** See Copyright Notice in license.html
*/

#include <string.h>

#include "lua.h"
#include "lauxlib.h"

#include "luasql.h"

#if !defined(lua_pushliteral)
#define lua_pushliteral(L, s) \
    lua_pushstring(L, "" s, (sizeof(s)/sizeof(char))-1)
#endif
