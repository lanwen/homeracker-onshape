annotation { "Feature Type Name" : "Compatibility regression" }
export const compatibilityRegression = defineFeature(function(context is Context, id is Id, definition is map)
    precondition {}
    {
        const kinds = [HomerackerKind.SUPPORT, HomerackerKind.CONNECTOR, HomerackerKind.PANEL,
                HomerackerKind.SLEEVE, HomerackerKind.FLAT_PANEL];
        const expectedCounts = [8, 30, 14, 5, 4];
        for (var old in [true, false])
        {
            for (var k = 0; k < size(kinds); k += 1)
            {
                var target = old ? Old::newContextWithDefaults() : newContextWithDefaults();
                const partId = newId() + "part";
                homeracker(target, partId, { "kind" : kinds[k] });
                const parts = qBodyType(qCreatedBy(partId, EntityType.BODY), BodyType.SOLID);
                const mates = qBodyType(qCreatedBy(partId, EntityType.BODY), BodyType.MATE_CONNECTOR);
                const mateQueries = evaluateQuery(target, mates);
                if (size(evaluateQuery(target, parts)) != 1 || size(mateQueries) != expectedCounts[k])
                    throw regenError("Incorrect part or mate count");
                if (size(evaluateQuery(target, qMateConnectorsOfParts(parts))) != expectedCounts[k])
                    throw regenError("Mate connector is missing its owner");
                const move = transform(vector(40, 50, 60) * millimeter) *
                        rotationAround(line(vector(0, 0, 0) * millimeter, vector(1, 2, 3)), 37 * degree);
                var frames = [];
                for (var mate in mateQueries)
                    frames = append(frames, evMateConnector(target, { "mateConnector" : mate }));
                opTransform(target, newId() + "move", { "bodies" : parts, "transform" : move });
                for (var i = 0; i < size(mateQueries); i += 1)
                {
                    const frame = evMateConnector(target, { "mateConnector" : mateQueries[i] });
                    if (!tolerantEquals(frame.origin, move * frames[i].origin) ||
                            !tolerantEquals(frame.xAxis, move.linear * frames[i].xAxis) ||
                            !tolerantEquals(frame.zAxis, move.linear * frames[i].zAxis))
                        throw regenError("Mate connector did not follow its part");
                }
                println({ "old" : old, "kind" : kinds[k], "mates" : size(frames) });
            }
        }
    });
